using System;
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
using Editors.Audio.AudioEditor.Events.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Events.WaveformVisualiser;
using Editors.Audio.Shared.Wwise;
using NAudio.Wave;
using Shared.Core.Events;
using Shared.Ui.Common;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public partial class WaveformVisualiserViewModel : ObservableObject, IDisposable
    {
        private readonly IEventHub _eventHub;
        private readonly ISoundEngine _soundEngine;
        private readonly IWaveformRendererService _waveformRendererService;
        private readonly IWaveformVisualisationCacheService _waveformVisualisationCacheService;

        private static readonly TimeSpan s_waveformResizeDebounceDelay = TimeSpan.FromMilliseconds(200);

        private readonly SemaphoreSlim _waveformRenderGate = new(1, 1);
        private readonly List<string> _currentPlaylistFilePaths = [];

        private bool _isWaveformPlayheadRenderingEnabled;
        private DateTime _lastFrameUtc;
        private double _visualSeconds;

        private CancellationTokenSource _waveformRenderCancellationTokenSource;
        private CancellationTokenSource _waveformResizeDebounceCancellationTokenSource;

        private DateTime _lastPlaybackTimerTextUpdateUtc = DateTime.MinValue;

        private string _currentFilePathKey;
        private int _currentPlaylistIndex = -1;
        private bool _isExplicitStopRequested;
        
        [ObservableProperty] private string _waveformVisualiserLabel;
        [ObservableProperty] private int _waveformPixelWidth;
        [ObservableProperty] private int _waveformPixelHeight;
        [ObservableProperty] private ImageSource _audioWaveformBaseImageSource;
        [ObservableProperty] private ImageSource _audioWaveformOverlayImageSource;
        [ObservableProperty] private Rect _audioWaveformOverlayClip;
        [ObservableProperty] private double _hostWidth;
        [ObservableProperty] private TimeSpan _currentPlaybackTime = TimeSpan.Zero;
        [ObservableProperty] private TimeSpan _totalPlaybackTime = TimeSpan.Zero;

        public WaveformVisualiserViewModel(
            IEventHub eventHub,
            ISoundEngine soundEngine,
            IWaveformRendererService waveformRendererService,
            IWaveformVisualisationCacheService waveformVisualisationCacheService)
        {
            _eventHub = eventHub;
            _soundEngine = soundEngine;
            _waveformRendererService = waveformRendererService;
            _waveformVisualisationCacheService = waveformVisualisationCacheService;

            _eventHub.Register<AudioFilesExplorerNodeSelectedEvent>(this, AudioFilesExplorerNodeSelected);
            _eventHub.Register<AudioFilesChangedEvent>(this, OnAudioFilesChanged);
            _eventHub.Register<PlayAudioRequestedEvent>(this, OnPlayAudioRequested);
            _eventHub.Register<CacheWaveformRequestedEvent>(this, OnCacheWaveformRequested);
            _eventHub.Register<DecacheWaveformRequestedEvent>(this, OnDecacheWaveformRequested);

            _soundEngine.PlaybackStopped += OnPlaybackStopped;

            AudioWaveformOverlayClip = new Rect(0, 0, 0, 0);

            UpdateWaveformVisualiserLabel();
        }

        public void AudioFilesExplorerNodeSelected(AudioFilesExplorerNodeSelectedEvent e) => SetSelectedPlaylist(e.WavFilePaths);

        public void OnAudioFilesChanged(AudioFilesChangedEvent e)
        {
            var wavFilePaths = e.AudioFiles
                .Select(audioFile => audioFile.WavPackFilePath)
                .Where(filePath => !string.IsNullOrWhiteSpace(filePath))
                .ToList();
            SetSelectedPlaylist(wavFilePaths);
        }

        public void OnPlayAudioRequested(PlayAudioRequestedEvent e)
        {
            SetSelectedPlaylist(e.WavFilePaths);
            PlayPause();
        }

        public void OnCacheWaveformRequested(CacheWaveformRequestedEvent e)
        {
            LoadWaveformImagesIntoCacheForCurrentWidth(e.FilePaths);
        }

        public void OnDecacheWaveformRequested(DecacheWaveformRequestedEvent e)
        {
            var filePathsInUse = new HashSet<string>(_currentPlaylistFilePaths, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_currentFilePathKey))
                filePathsInUse.Add(_currentFilePathKey);

            foreach (var filePath in e.FilePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                if (filePathsInUse.Contains(filePath))
                    continue;

                _waveformVisualisationCacheService.Remove(filePath);
            }
        }

        public void SetSelectedPlaylist(List<string> filePaths)
        {
            StopWaveformPlayheadRendering();
            _soundEngine.Stop();

            _currentPlaylistFilePaths.Clear();
            if (filePaths != null)
            {
                var validDistinctPaths = filePaths
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var path in validDistinctPaths)
                    _currentPlaylistFilePaths.Add(path);
            }

            if (_currentPlaylistFilePaths.Count > 0)
                _currentPlaylistIndex = 0;
            else
                _currentPlaylistIndex = -1;

            _visualSeconds = 0;

            CurrentPlaybackTime = TimeSpan.Zero;

            LoadWaveformImagesIntoCacheForCurrentWidth(_currentPlaylistFilePaths);

            if (_currentPlaylistIndex >= 0)
            {
                _currentFilePathKey = _currentPlaylistFilePaths[_currentPlaylistIndex];
                UpdateWaveformVisualiserLabel();
                UpdateTotalPlaybackTimeFromFilePath(_currentFilePathKey);
                ResetWaveformPlayheadAndProgress();
                _ = RenderWaveformPreviewAsync();
            }
            else
            {
                _currentFilePathKey = string.Empty;
                UpdateWaveformVisualiserLabel();
                UpdateTotalPlaybackTimeFromFilePath(_currentFilePathKey);
                ResetWaveformPlayheadAndProgress();
            }
        }

        partial void OnHostWidthChanged(double value)
        {
            var previousCancellationToken = Interlocked.Exchange(ref _waveformResizeDebounceCancellationTokenSource, new CancellationTokenSource());
            if (previousCancellationToken != null)
            {
                previousCancellationToken.Cancel();
                previousCancellationToken.Dispose();
            }

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

            var filePathsNeedingRebuild = _currentPlaylistFilePaths
                .Where(filePath => _waveformVisualisationCacheService.GetWaveformVisualisation(filePath, targetWidth) == null)
                .Where(filePath => !string.Equals(filePath, _currentFilePathKey, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (filePathsNeedingRebuild.Length == 0)
                return;

            LoadWaveformImagesIntoCacheForCurrentWidth(filePathsNeedingRebuild);
        }

        [RelayCommand] private void PlayPause()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePathKey))
                return;

            if (_soundEngine.PlaybackState == PlaybackState.Stopped)
            {
                _soundEngine.LoadFromFilePath(_currentFilePathKey);

                _visualSeconds = 0;
                CurrentPlaybackTime = TimeSpan.Zero;
                ResetWaveformPlayheadAndProgress();
            }

            _soundEngine.PlayPause();

            if (_soundEngine.PlaybackState == PlaybackState.Playing)
                StartWaveformPlayheadRendering();
            else
                StopWaveformPlayheadRendering();
        }

        private async Task RenderWaveformPreviewAsync()
        {
            var previousCancellationToken = Interlocked.Exchange(ref _waveformRenderCancellationTokenSource, new CancellationTokenSource());
            if (previousCancellationToken != null)
            {
                previousCancellationToken.Cancel();
                previousCancellationToken.Dispose();
            }

            var cancellationToken = _waveformRenderCancellationTokenSource.Token;

            if (string.IsNullOrWhiteSpace(_currentFilePathKey))
                return;

            await _waveformRenderGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var targetWidth = GetTargetWidth();

                var cachedResult = _waveformVisualisationCacheService.GetWaveformVisualisation(_currentFilePathKey, targetWidth);
                if (cachedResult != null)
                {
                    ApplyWaveformBitmaps(cachedResult.Visualisation.BaseImage, cachedResult.Visualisation.OverlayImage);
                    TotalPlaybackTime = cachedResult.TotalTime;
                    return;
                }

                var result = await _waveformRendererService.RenderAsync(_currentFilePathKey, targetWidth, cancellationToken).ConfigureAwait(false);
                _waveformVisualisationCacheService.Store(_currentFilePathKey, result);

                ApplyWaveformBitmaps(result.Visualisation.BaseImage, result.Visualisation.OverlayImage);
                TotalPlaybackTime = result.TotalTime;
            }
            finally
            {
                _waveformRenderGate.Release();
            }
        }


        private void ApplyWaveformBitmaps(BitmapImage baseImage, BitmapImage overlayImage)
        {
            void Apply()
            {
                AudioWaveformBaseImageSource = baseImage;
                AudioWaveformOverlayImageSource = overlayImage;

                WaveformPixelWidth = baseImage.PixelWidth;
                WaveformPixelHeight = baseImage.PixelHeight;

                AudioWaveformOverlayClip = new Rect(0, 0, 0, WaveformPixelHeight);
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(Apply);
            else
                Apply();
        }

        private int GetTargetWidth()
        {
            var hostWidth = HostWidth;
            if (hostWidth > 0)
                return (int)Math.Max(300, hostWidth);
            return 800;
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

        private void OnCompositionTargetRenderingForWaveformPlayhead(object sender, EventArgs e)
        {
            if (_soundEngine == null || WaveformPixelWidth <= 0)
                return;

            var totalTime = TotalPlaybackTime;
            if (totalTime <= TimeSpan.Zero)
                return;

            var timeNow = DateTime.UtcNow;
            var secondsSinceLastFrame = (timeNow - _lastFrameUtc).TotalSeconds;
            _lastFrameUtc = timeNow;
            if (secondsSinceLastFrame <= 0)
                return;

            var deviceTimeSeconds = _soundEngine.GetDeviceAlignedTimeNow().TotalSeconds;

            _visualSeconds += secondsSinceLastFrame;
            var error = deviceTimeSeconds - _visualSeconds;
            var positionConvergenceGain = 0.15;
            _visualSeconds += positionConvergenceGain * error;

            if (_visualSeconds < 0)
                _visualSeconds = 0;

            if (_visualSeconds > totalTime.TotalSeconds)
                _visualSeconds = totalTime.TotalSeconds;

            var ratio = _visualSeconds / totalTime.TotalSeconds;
            var playedWidthPx = ratio * WaveformPixelWidth;

            AudioWaveformOverlayClip = new Rect(0, 0, playedWidthPx, WaveformPixelHeight);

            if ((timeNow - _lastPlaybackTimerTextUpdateUtc).TotalMilliseconds >= 50)
            {
                _lastPlaybackTimerTextUpdateUtc = timeNow;
                CurrentPlaybackTime = TimeSpan.FromSeconds(_visualSeconds);
            }
        }

        public void SetSelectedFilePath(string filePath)
        {
            StopWaveformPlayheadRendering();
            _soundEngine.Stop();

            _visualSeconds = 0;

            _currentFilePathKey = filePath;
            UpdateWaveformVisualiserLabel();
            UpdateTotalPlaybackTimeFromFilePath(_currentFilePathKey);

            CurrentPlaybackTime = TimeSpan.Zero;

            ResetWaveformPlayheadAndProgress();
            _ = RenderWaveformPreviewAsync();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var wasExplicitStop = _isExplicitStopRequested;
                    _isExplicitStopRequested = false;

                    ResetWaveformPlayheadAndProgress();
                    StopWaveformPlayheadRendering();
                    CurrentPlaybackTime = TimeSpan.Zero;

                    if (wasExplicitStop)
                        return;

                    if (e != null && e.Exception != null)
                        return;

                    if (_currentPlaylistFilePaths.Count == 0)
                        return;

                    var nextIndex = _currentPlaylistIndex + 1;
                    if (nextIndex >= 0 && nextIndex < _currentPlaylistFilePaths.Count)
                    {
                        _currentPlaylistIndex = nextIndex;
                        var nextPath = _currentPlaylistFilePaths[_currentPlaylistIndex];

                        SetSelectedFilePath(nextPath);
                        PlayPause();
                    }
                }
                finally { }
            });
        }

        private void ResetWaveformPlayheadAndProgress() => AudioWaveformOverlayClip = new Rect(0, 0, 0, WaveformPixelHeight);

        private void LoadWaveformImagesIntoCacheForCurrentWidth(IEnumerable<string> filePaths)
        {
            var targetWidth = GetTargetWidth();

            var cancellationTokenSource = new CancellationTokenSource();
            _ = _waveformVisualisationCacheService.PreloadWaveformVisualisationsAsync(filePaths, targetWidth, _waveformRendererService, cancellationTokenSource.Token);
        }

        private void UpdateWaveformVisualiserLabel()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePathKey))
                WaveformVisualiserLabel = "Sound Engine";
            else
            {
                var fileName = Path.GetFileName(_currentFilePathKey);
                WaveformVisualiserLabel = $"Sound Engine – {WpfHelpers.DuplicateUnderscores(fileName)}";
            }
        }

        private void UpdateTotalPlaybackTimeFromFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                TotalPlaybackTime = TimeSpan.Zero;
                return;
            }

            try
            {
                using var reader = new WaveFileReader(filePath);
                TotalPlaybackTime = reader.TotalTime;
            }
            catch
            {
                TotalPlaybackTime = TimeSpan.Zero;
            }
        }

        public void Dispose()
        {
            StopWaveformPlayheadRendering();
            _soundEngine.Dispose();

            if (_waveformRenderCancellationTokenSource != null)
            {
                _waveformRenderCancellationTokenSource.Cancel();
                _waveformRenderCancellationTokenSource.Dispose();
            }

            if (_waveformResizeDebounceCancellationTokenSource != null)
            {
                _waveformResizeDebounceCancellationTokenSource.Cancel();
                _waveformResizeDebounceCancellationTokenSource.Dispose();
            }
        }

        public void SetSelectedHostWidth(double width) => HostWidth = width;
    }
}
