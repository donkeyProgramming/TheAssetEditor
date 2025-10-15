using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Events;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public partial class WaveformVisualiserViewModel : ObservableObject, IDisposable
    {
        private readonly IEventHub _eventHub;
        private readonly IPackFileService _packFileService;

        private IWavePlayer _audioWaveOutputDevice;
        private WaveStream _waveReader;
        private MemoryStream _audioDataStream;

        private bool _isWaveformPlayheadRenderingEnabled;
        private DateTime _lastFrameUtc;
        private double _visualSeconds;

        private long _deviceBytesAtLastPlayOrResume;
        private TimeSpan _readerTimeAtLastPlayOrResume = TimeSpan.Zero;

        private readonly SemaphoreSlim _waveformRenderGate = new(1, 1);
        private CancellationTokenSource _waveformRenderCancellationTokenSource;


        private CancellationTokenSource _waveformResizeDebounceCancellationTokenSource;
        private static readonly TimeSpan s_waveformResizeDebounceDelay = TimeSpan.FromMilliseconds(200);

        private DateTime _lastPlaybackTimerTextUpdateUtc = DateTime.MinValue;

        [ObservableProperty] private int _waveformPixelWidth;
        [ObservableProperty] private int _waveformPixelHeight;
        [ObservableProperty] private ImageSource _audioWaveformBaseImageSource;
        [ObservableProperty] private ImageSource _audioWaveformOverlayImageSource;
        [ObservableProperty] private Rect _audioWaveformOverlayClip;
        [ObservableProperty] private double _hostWidth;
        [ObservableProperty] private TimeSpan _currentPlaybackTime = TimeSpan.Zero;
        [ObservableProperty] private TimeSpan _totalPlaybackTime = TimeSpan.Zero;


        private readonly ConcurrentDictionary<string, WaveformVisualisation> _waveformVisualisationByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _waveformPrecomputeInflightByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _waveformCacheEvictedByFilePath = new();
        private string _currentFilePathKey;


        public WaveformVisualiserViewModel(IEventHub eventHub, IPackFileService packFileService)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;

            _eventHub.Register<DisplayWaveformVisualiserRequestedEvent>(this, OnDisplayWaveformVisualiserRequestedEvent);
            _eventHub.Register<AddToWaveformCacheRequestedEvent>(this, OnAddToWaveformCacheRequestedEvent);
            _eventHub.Register<RemoveFromWaveformCacheRequestedEvent>(this, OnRemoveFromWaveformCacheRequestedEvent);

            // Initialise the clip to zero width so the overlay is hidden until playback or selection
            AudioWaveformOverlayClip = new Rect(0, 0, 0, 0);
        }

        // Selection
        public void OnDisplayWaveformVisualiserRequestedEvent(DisplayWaveformVisualiserRequestedEvent e)
        {
            WarmCacheForCurrentWidth([e.FilePath]);
            SetSelectedFilePath(e.FilePath);
        }

        public void OnAddToWaveformCacheRequestedEvent(AddToWaveformCacheRequestedEvent e)
        {
            WarmCacheForCurrentWidth(e.FilePaths);
        }

        public void OnRemoveFromWaveformCacheRequestedEvent(RemoveFromWaveformCacheRequestedEvent e)
        {
            foreach (var filePath in e.FilePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                if (string.Equals(filePath, _currentFilePathKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                _waveformCacheEvictedByFilePath[filePath] = 0;
                _waveformVisualisationByFilePath.TryRemove(filePath, out _);
                _waveformPrecomputeInflightByFilePath.TryRemove(filePath, out _);
            }
        }

        partial void OnHostWidthChanged(double value)
        {
            var previousCancellationTokenSource = Interlocked.Exchange(ref _waveformResizeDebounceCancellationTokenSource, new CancellationTokenSource());
            previousCancellationTokenSource?.Cancel();
            previousCancellationTokenSource?.Dispose();

            var cancellationToken = _waveformResizeDebounceCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(s_waveformResizeDebounceDelay, cancellationToken).ConfigureAwait(false);
                    RebuildCacheForCurrentWidthExcludingCurrent();

                    if (!string.IsNullOrWhiteSpace(_currentFilePathKey))
                        await RenderWaveformPreviewAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            });
        }

        private void RebuildCacheForCurrentWidthExcludingCurrent()
        {
            var targetWidth = GetTargetWidth();

            var keysNeedingRebuild = _waveformVisualisationByFilePath
                .Where(kvp => kvp.Value.PixelWidth != targetWidth)
                .Select(kvp => kvp.Key)
                .Where(key => !string.Equals(key, _currentFilePathKey, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (keysNeedingRebuild.Length == 0)
                return;

            WarmCacheForCurrentWidth(keysNeedingRebuild);
        }

        [RelayCommand] private void PlayPause()
        {
            if (_audioWaveOutputDevice == null)
            {
                EnsureReaderFromSelectedFilePath();

                try
                {
                    _audioWaveOutputDevice = new WasapiOut(AudioClientShareMode.Shared, true, 100);
                }
                catch
                {
                    _audioWaveOutputDevice = new WaveOutEvent { DesiredLatency = 150, NumberOfBuffers = 3 };
                }

                _audioWaveOutputDevice.Init(_waveReader);
                _audioWaveOutputDevice.PlaybackStopped += OnPlaybackStopped;

                _readerTimeAtLastPlayOrResume = _waveReader.CurrentTime;
                _deviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _visualSeconds = _readerTimeAtLastPlayOrResume.TotalSeconds;

                // Initialise total time now that the reader is ready
                TotalPlaybackTime = _waveReader.TotalTime;
                CurrentPlaybackTime = _waveReader.CurrentTime;

                _audioWaveOutputDevice.Play();
                StartWaveformPlayheadRendering();
                return;
            }

            if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Playing)
            {
                _readerTimeAtLastPlayOrResume = GetDeviceAlignedTimeNow();

                _audioWaveOutputDevice.Pause();
                StopWaveformPlayheadRendering();
            }
            else if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Paused)
            {
                _deviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _visualSeconds = _readerTimeAtLastPlayOrResume.TotalSeconds;

                _audioWaveOutputDevice.Play();
                StartWaveformPlayheadRendering();
            }
            else if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Stopped)
            {
                _waveReader.Position = 0;
                _readerTimeAtLastPlayOrResume = TimeSpan.Zero;
                _deviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _visualSeconds = 0;

                _audioWaveOutputDevice.Play();
                StartWaveformPlayheadRendering();
            }
        }

        private async Task RenderWaveformPreviewAsync()
        {
            // perhaps put the previousCancellationTokenSource stuff in a helper
            var previousCancellationTokenSource = Interlocked.Exchange(ref _waveformRenderCancellationTokenSource, new CancellationTokenSource());
            previousCancellationTokenSource?.Cancel();
            previousCancellationTokenSource?.Dispose();

            var cancellationToken = _waveformRenderCancellationTokenSource.Token;

            if (string.IsNullOrWhiteSpace(_currentFilePathKey))
                return;

            await _waveformRenderGate.WaitAsync(cancellationToken);

            try
            {
                var targetWidth = GetTargetWidth();

                var baseSettings = WaveformVisualiserHelpers.CreateBaseWaveformSettings(targetWidth);
                var overlaySettings = WaveformVisualiserHelpers.CreateOverlayWaveformSettings(targetWidth);

                if (_waveformVisualisationByFilePath.TryGetValue(_currentFilePathKey, out var cached) &&
                    cached.PixelWidth == targetWidth)
                {
                    ApplyWaveformBitmaps(cached.BaseImage, cached.OverlayImage);
                    return;
                }

                var (baseImage, overlayImage) = await Task.Run(() =>
                {
                    var packFile = _packFileService.FindFile(_currentFilePathKey);
                    var data = packFile.DataSource.ReadData();

                    using var memoryStream = new MemoryStream(data, writable: false);
                    using var reader = WaveformVisualiserHelpers.CreateWaveStream(memoryStream, packFile.Extension);
                    using var aligned = new BlockAlignReductionStream(reader);

                    var renderer = new WaveFormRenderer();
                    using var baseImage = renderer.Render(aligned, baseSettings);
                    aligned.Position = 0;
                    using var overlayImage = renderer.Render(aligned, overlaySettings);

                    var baseBmp = WaveformVisualiserHelpers.ToBitmapImage(baseImage);
                    var overlayBmp = WaveformVisualiserHelpers.ToBitmapImage(overlayImage);
                    return (baseBmp, overlayBmp);
                }, cancellationToken).ConfigureAwait(false);

                _waveformVisualisationByFilePath[_currentFilePathKey] = WaveformVisualisation.Create(baseImage, overlayImage);

                ApplyWaveformBitmaps(baseImage, overlayImage);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _waveformRenderGate.Release();
            }
        }

        private void ApplyWaveformBitmaps(BitmapImage baseImage, BitmapImage overlayImage)
        {
            // Marshal to UI if needed
            void Do()
            {
                AudioWaveformBaseImageSource = baseImage;
                AudioWaveformOverlayImageSource = overlayImage;

                WaveformPixelWidth = baseImage.PixelWidth;
                WaveformPixelHeight = baseImage.PixelHeight;

                AudioWaveformOverlayClip = new Rect(0, 0, 0, WaveformPixelHeight);
            }
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) dispatcher.Invoke(Do); else Do();
        }

        private int GetTargetWidth()
        {
            var hostWidth = HostWidth;
            return (int)Math.Max(300, hostWidth > 0 ? hostWidth : 800);
        }

        private void StartWaveformPlayheadRendering()
        {
            if (_isWaveformPlayheadRenderingEnabled)
                return;

            _lastFrameUtc = DateTime.UtcNow;
            CompositionTarget.Rendering += OnCompositionTargetRenderingForWaveformPlayhead;
            _isWaveformPlayheadRenderingEnabled = true;
        }

        private void StopWaveformPlayheadRendering()
        {
            if (!_isWaveformPlayheadRenderingEnabled)
                return;

            CompositionTarget.Rendering -= OnCompositionTargetRenderingForWaveformPlayhead;
            _isWaveformPlayheadRenderingEnabled = false;
        }

        private void OnCompositionTargetRenderingForWaveformPlayhead(object? sender, EventArgs e)
        {
            if (_waveReader == null || _audioWaveOutputDevice == null || WaveformPixelWidth <= 0)
                return;

            var total = _waveReader.TotalTime;
            if (total <= TimeSpan.Zero)
                return;

            var now = DateTime.UtcNow;
            var dt = (now - _lastFrameUtc).TotalSeconds;
            _lastFrameUtc = now;
            if (dt <= 0)
                return;

            var deviceBytesAbsolute = GetDeviceBytes();
            var elapsedBytes = Math.Max(0, deviceBytesAbsolute - _deviceBytesAtLastPlayOrResume);
            var secondsFromDevice = elapsedBytes / (double)GetDeviceBytesPerSecond();
            var deviceSeconds = (_readerTimeAtLastPlayOrResume + TimeSpan.FromSeconds(secondsFromDevice)).TotalSeconds;

            _visualSeconds += dt;
            var error = deviceSeconds - _visualSeconds;
            var positionConvergenceGain = 0.15;
            _visualSeconds += positionConvergenceGain * error;

            if (_visualSeconds < 0) _visualSeconds = 0;
            if (_visualSeconds > total.TotalSeconds) _visualSeconds = total.TotalSeconds;

            var ratio = _visualSeconds / total.TotalSeconds;
            var playedWidthPx = ratio * WaveformPixelWidth;

            // Update the clip
            AudioWaveformOverlayClip = new Rect(0, 0, playedWidthPx, WaveformPixelHeight);

            if ((now - _lastPlaybackTimerTextUpdateUtc).TotalMilliseconds >= 50)
            {
                _lastPlaybackTimerTextUpdateUtc = now;
                CurrentPlaybackTime = TimeSpan.FromSeconds(_visualSeconds);
            }
        }

        public void SetSelectedFilePath(string filePath)
        {
            StopWaveformPlayheadRendering();
            DisposePlayback();

            _readerTimeAtLastPlayOrResume = TimeSpan.Zero;
            _deviceBytesAtLastPlayOrResume = 0;
            _visualSeconds = 0;

            _currentFilePathKey = filePath ?? throw new ArgumentNullException(nameof(filePath));

            // Things like resetting these should be in helpers
            CurrentPlaybackTime = TimeSpan.Zero;
            TotalPlaybackTime = TimeSpan.Zero;

            ResetWaveformPlayheadAndProgress();
            _ = RenderWaveformPreviewAsync();
        }

        [RelayCommand]
        private void Stop()
        {
            if (_audioWaveOutputDevice == null)
                return;

            _audioWaveOutputDevice.Stop();
            if (_waveReader != null)
                _waveReader.Position = 0;

            _readerTimeAtLastPlayOrResume = TimeSpan.Zero;
            _deviceBytesAtLastPlayOrResume = 0;
            _visualSeconds = 0;
            StopWaveformPlayheadRendering();
            ResetWaveformPlayheadAndProgress();

            // Things like resetting these should be in helpers
            CurrentPlaybackTime = TimeSpan.Zero;
        }


        // Reader / Device helpers
        private void EnsureReaderFromSelectedFilePath()
        {
            DisposeReaderOnly();

            if (string.IsNullOrWhiteSpace(_currentFilePathKey))
                throw new InvalidOperationException("No file path selected for WaveformVisualiser.");

            var packFile = _packFileService.FindFile(_currentFilePathKey)
                           ?? throw new FileNotFoundException($"PackFile not found for path '{_currentFilePathKey}'.");

            var data = packFile.DataSource.ReadData();
            if (data is not { Length: > 0 })
                throw new InvalidOperationException("No audio bytes provided to WaveformVisualiser.");

            _audioDataStream = new MemoryStream(data, writable: false);
            _waveReader = WaveformVisualiserHelpers.CreateWaveStream(_audioDataStream, packFile.Extension);
        }

        private TimeSpan GetDeviceAlignedTimeNow()
        {
            var bytesWrittenAbsolute = GetDeviceBytes();
            var elapsedBytes = Math.Max(0, bytesWrittenAbsolute - _deviceBytesAtLastPlayOrResume);
            var secondsFromDevice = elapsedBytes / (double)GetDeviceBytesPerSecond();
            return _readerTimeAtLastPlayOrResume + TimeSpan.FromSeconds(secondsFromDevice);
        }

        private long GetDeviceBytes() => _audioWaveOutputDevice is IWavePosition pos ? pos.GetPosition() : 0L;

        private int GetDeviceBytesPerSecond()
        {
            if (_audioWaveOutputDevice is IWavePosition position)
                return position.OutputWaveFormat.AverageBytesPerSecond;
            return _waveReader?.WaveFormat.AverageBytesPerSecond ?? 1;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ResetWaveformPlayheadAndProgress();
                StopWaveformPlayheadRendering();
                CurrentPlaybackTime = TimeSpan.Zero;
            });
        }


        private void ResetWaveformPlayheadAndProgress()
        {
            AudioWaveformOverlayClip = new Rect(0, 0, 0, WaveformPixelHeight);
        }

        private void WarmCacheForCurrentWidth(IEnumerable<string> filePaths)
        {
            var targetWidth = GetTargetWidth();

            var uniquePaths = (filePaths ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(p => !_waveformVisualisationByFilePath.TryGetValue(p, out var existing) || existing.PixelWidth != targetWidth)
                .Where(p => _waveformPrecomputeInflightByFilePath.TryAdd(p, 0))
                .ToArray();

            if (uniquePaths.Length == 0)
                return;

            _ = Task.Run(async () =>
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
                };

                await Parallel.ForEachAsync(uniquePaths, options, async (filePath, _) =>
                {
                    try
                    {
                        _waveformCacheEvictedByFilePath.TryRemove(filePath, out var _);

                        var packFile = _packFileService.FindFile(filePath);
                        if (packFile == null)
                            return;

                        var data = packFile.DataSource.ReadData();
                        if (data == null || data.Length == 0)
                            return;

                        var baseSettings = WaveformVisualiserHelpers.CreateBaseWaveformSettings(targetWidth);
                        var overlaySettings = WaveformVisualiserHelpers.CreateOverlayWaveformSettings(targetWidth);

                        using var ms = new MemoryStream(data, writable: false);
                        using var reader = WaveformVisualiserHelpers.CreateWaveStream(ms, packFile.Extension);
                        using var aligned = new BlockAlignReductionStream(reader);

                        var renderer = new WaveFormRenderer();

                        using var baseImg = renderer.Render(aligned, baseSettings);
                        aligned.Position = 0;
                        using var overlayImg = renderer.Render(aligned, overlaySettings);

                        var baseImage = WaveformVisualiserHelpers.ToBitmapImage(baseImg);
                        var overlayImage = WaveformVisualiserHelpers.ToBitmapImage(overlayImg);

                        if (_waveformCacheEvictedByFilePath.ContainsKey(filePath))
                            return;

                        _waveformVisualisationByFilePath[filePath] = WaveformVisualisation.Create(baseImage, overlayImage);
                    }
                    finally
                    {
                        _waveformPrecomputeInflightByFilePath.TryRemove(filePath, out var _);
                        await Task.CompletedTask;
                    }
                });
            });
        }

        // Disposal
        private void DisposePlayback()
        {
            if (_audioWaveOutputDevice != null)
            {
                _audioWaveOutputDevice.Stop();
                _audioWaveOutputDevice.Dispose();
                _audioWaveOutputDevice = null;
            }

            DisposeReaderOnly();
        }

        private void DisposeReaderOnly()
        {
            if (_waveReader != null)
            {
                _waveReader.Dispose();
                _waveReader = null;
            }

            if (_audioDataStream != null)
            {
                _audioDataStream.Dispose();
                _audioDataStream = null;
            }
        }

        public void Dispose()
        {
            StopWaveformPlayheadRendering();
            DisposePlayback();

            _waveformRenderCancellationTokenSource?.Cancel();
            _waveformRenderCancellationTokenSource?.Dispose();

            _waveformResizeDebounceCancellationTokenSource?.Cancel();
            _waveformResizeDebounceCancellationTokenSource?.Dispose();
        }

        public void SetSelectedHostWidth(double width) => HostWidth = width;
    }
}
