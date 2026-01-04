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

        private IWavePlayer _audioWaveOutputDevice;
        private WaveFileReader _waveReader;
        private MemoryStream _audioDataStream;

        public TimeSpan TotalPlaybackTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan ReaderTimeAtLastPlayOrResume { get; private set; } = TimeSpan.Zero;
        public long DeviceBytesAtLastPlayOrResume { get; private set; } = 0;

        public PlaybackState PlaybackState
        {
            get
            {
                if (_audioWaveOutputDevice == null)
                    return PlaybackState.Stopped;
                return _audioWaveOutputDevice.PlaybackState;
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

            _audioDataStream = new MemoryStream(data, writable: false);
            _waveReader = new WaveFileReader(_audioDataStream);

            TotalPlaybackTime = _waveReader.TotalTime;
            ReaderTimeAtLastPlayOrResume = _waveReader.CurrentTime;
            DeviceBytesAtLastPlayOrResume = 0;

            if (_audioWaveOutputDevice != null)
            {
                _audioWaveOutputDevice.PlaybackStopped -= OnPlaybackStoppedForward;
                _audioWaveOutputDevice.Dispose();
                _audioWaveOutputDevice = null;
            }
        }

        public void PlayPause()
        {
            if (_audioWaveOutputDevice == null)
            {
                EnsureOutputDeviceCreated();

                _audioWaveOutputDevice.Init(_waveReader);
                _audioWaveOutputDevice.PlaybackStopped += OnPlaybackStoppedForward;

                ReaderTimeAtLastPlayOrResume = _waveReader.CurrentTime;
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();

                TotalPlaybackTime = _waveReader.TotalTime;

                _audioWaveOutputDevice.Play();
                return;
            }

            if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Playing)
            {
                ReaderTimeAtLastPlayOrResume = GetDeviceAlignedTimeNow();
                _audioWaveOutputDevice.Pause();
            }
            else if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Paused)
            {
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _audioWaveOutputDevice.Play();
            }
            else if (_audioWaveOutputDevice.PlaybackState == PlaybackState.Stopped)
            {
                _waveReader.Position = 0;
                ReaderTimeAtLastPlayOrResume = TimeSpan.Zero;
                DeviceBytesAtLastPlayOrResume = GetDeviceBytes();
                _audioWaveOutputDevice.Play();
            }
        }

        public void Stop()
        {
            if (_audioWaveOutputDevice == null)
                return;

            _audioWaveOutputDevice.Stop();
            if (_waveReader != null)
                _waveReader.Position = 0;

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
            if (_audioWaveOutputDevice is IWavePosition position)
                return position.GetPosition();
            return 0L;
        }

        public int GetDeviceBytesPerSecond()
        {
            if (_audioWaveOutputDevice is IWavePosition position)
                return position.OutputWaveFormat.AverageBytesPerSecond;
            if (_waveReader != null)
                return _waveReader.WaveFormat.AverageBytesPerSecond;
            return 1;
        }

        private void EnsureOutputDeviceCreated()
        {
            if (_audioWaveOutputDevice != null)
                return;

            try
            {
                _audioWaveOutputDevice = new WasapiOut(AudioClientShareMode.Shared, true, 100);
            }
            catch
            {
                _audioWaveOutputDevice = new WaveOutEvent { DesiredLatency = 150, NumberOfBuffers = 3 };
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
            if (_audioWaveOutputDevice != null)
            {
                _audioWaveOutputDevice.PlaybackStopped -= OnPlaybackStoppedForward;
                _audioWaveOutputDevice.Stop();
                _audioWaveOutputDevice.Dispose();
                _audioWaveOutputDevice = null;
            }

            DisposeReaderOnly();
        }
    }
}
