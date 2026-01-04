using System;
using System.IO;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Shared.Core.PackFiles;

namespace Editors.Audio.Shared.Wwise
{
    public interface ISoundEngine : IDisposable
    {
        TimeSpan TotalPlaybackTime { get; }
        TimeSpan ReaderTimeAtLastPlayOrResume { get; }
        long DeviceBytesAtLastPlayOrResume { get; }
        PlaybackState PlaybackState { get; }
        void LoadFromFilePath(string filePath);
        void Stop();
        void PlayPause();
        TimeSpan GetDeviceAlignedTimeNow();
        int GetDeviceBytesPerSecond();
        long GetDeviceBytes();
        event EventHandler<StoppedEventArgs> PlaybackStopped;
    }

    public class SoundEngine(IPackFileService packFileService) : ISoundEngine
    {
        private readonly IPackFileService _packFileService = packFileService;

        private IWavePlayer _wavePlayer;
        private WaveFileReader _waveFileReader;
        private MemoryStream _memoryStream;

        public TimeSpan TotalPlaybackTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan ReaderTimeAtLastPlayOrResume { get; private set; } = TimeSpan.Zero;
        public long DeviceBytesAtLastPlayOrResume { get; private set; } = 0;

        public PlaybackState PlaybackState
        {
            get
            {
                if (_wavePlayer == null)
                    return PlaybackState.Stopped;
                return _wavePlayer.PlaybackState;
            }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void LoadFromFilePath(string filePath)
        {
            DisposeReaderOnly();

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var packFile = _packFileService.FindFile(filePath);
            var data = packFile.DataSource.ReadData();

            _memoryStream = new MemoryStream(data, writable: false);
            _waveFileReader = new WaveFileReader(_memoryStream);

            TotalPlaybackTime = _waveFileReader.TotalTime;
            ReaderTimeAtLastPlayOrResume = _waveFileReader.CurrentTime;
            DeviceBytesAtLastPlayOrResume = 0;

            if (_wavePlayer != null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStoppedForward;
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }
        }

        public void PlayPause()
        {
            if (_wavePlayer == null)
            {
                EnsureOutputDeviceCreated();

                _wavePlayer.Init(_waveFileReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStoppedForward;

                ReaderTimeAtLastPlayOrResume = _waveFileReader.CurrentTime;
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();

                TotalPlaybackTime = _waveFileReader.TotalTime;

                _wavePlayer.Play();
                return;
            }

            if (_wavePlayer.PlaybackState == PlaybackState.Playing)
            {
                ReaderTimeAtLastPlayOrResume = GetDeviceAlignedTimeNow();
                _wavePlayer.Pause();
            }
            else if (_wavePlayer.PlaybackState == PlaybackState.Paused)
            {
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _wavePlayer.Play();
            }
            else if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
            {
                _waveFileReader.Position = 0;
                ReaderTimeAtLastPlayOrResume = TimeSpan.Zero;
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _wavePlayer.Play();
            }
        }

        public void Stop()
        {
            if (_wavePlayer == null)
                return;

            _wavePlayer.Stop();
            if (_waveFileReader != null)
                _waveFileReader.Position = 0;

            ReaderTimeAtLastPlayOrResume = TimeSpan.Zero;
            DeviceBytesAtLastPlayOrResume = 0;
        }

        public TimeSpan GetDeviceAlignedTimeNow()
        {
            var bytesWrittenAbsolute = GetDeviceBytes();
            var elapsedBytes = Math.Max(0, bytesWrittenAbsolute - DeviceBytesAtLastPlayOrResume);
            var secondsFromDevice = elapsedBytes / (double)GetDeviceBytesPerSecond();
            return ReaderTimeAtLastPlayOrResume + TimeSpan.FromSeconds(secondsFromDevice);
        }

        public long GetDeviceBytes()
        {
            if (_wavePlayer is IWavePosition position)
                return position.GetPosition();
            return 0L;
        }

        public int GetDeviceBytesPerSecond()
        {
            if (_wavePlayer is IWavePosition position)
                return position.OutputWaveFormat.AverageBytesPerSecond;
            if (_waveFileReader != null)
                return _waveFileReader.WaveFormat.AverageBytesPerSecond;
            return 1;
        }

        private void EnsureOutputDeviceCreated()
        {
            if (_wavePlayer != null)
                return;

            try
            {
                _wavePlayer = new WasapiOut(AudioClientShareMode.Shared, true, 100);
            }
            catch
            {
                _wavePlayer = new WaveOutEvent { DesiredLatency = 150, NumberOfBuffers = 3 };
            }
        }

        private void OnPlaybackStoppedForward(object sender, StoppedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(() => PlaybackStopped?.Invoke(this, e));
            else
                PlaybackStopped?.Invoke(this, e);
        }

        private void DisposeReaderOnly()
        {
            if (_waveFileReader != null)
            {
                _waveFileReader.Dispose();
                _waveFileReader = null;
            }

            if (_memoryStream != null)
            {
                _memoryStream.Dispose();
                _memoryStream = null;
            }
        }

        public void Dispose()
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStoppedForward;
                _wavePlayer.Stop();
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }

            DisposeReaderOnly();
        }
    }
}
