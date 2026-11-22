using System;
using System.IO;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Shared.Core.PackFiles;
using Shared.GameFormats.Audio.Containers.Ogg;

namespace Editors.Audio.Shared.Wwise
{
    public interface ISoundEngine : IDisposable
    {
        TimeSpan TotalPlaybackTime { get; }
        TimeSpan ReaderTimeAtLastPlayOrResume { get; }
        long DeviceBytesAtLastPlayOrResume { get; }
        PlaybackState PlaybackState { get; }
        void LoadFromFilePath(string filePath);
        void LoadFromWemBytes(byte[] wemBytes);
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

        // PCM subformat GUIDs a WAVE_FORMAT_EXTENSIBLE header can carry
        private static readonly Guid s_pcmSubFormat = new("00000001-0000-0010-8000-00aa00389b71");
        private static readonly Guid s_ieeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");

        private IWavePlayer _wavePlayer;
        private WaveStream _waveFileReader;
        private WaveStream _compatibleWaveStream;
        private IWaveProvider _playbackProvider;
        private MemoryStream _memoryStream;

        public TimeSpan TotalPlaybackTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan ReaderTimeAtLastPlayOrResume { get; private set; } = TimeSpan.Zero;
        public long DeviceBytesAtLastPlayOrResume { get; private set; } = 0;

        public PlaybackState PlaybackState => _wavePlayer?.PlaybackState ?? PlaybackState.Stopped;

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void LoadFromFilePath(string filePath)
        {
            DisposeReaderOnly();

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var packFile = _packFileService.FindFile(filePath);
            var data = packFile.DataSource.ReadData();

            _memoryStream = new MemoryStream(data, writable: false);
            var reader = new WaveFileReader(_memoryStream);
            _waveFileReader = reader;
            _playbackProvider = CreatePlaybackProvider(reader, out _compatibleWaveStream);

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

        public void LoadFromWemBytes(byte[] wemBytes)
        {
            DisposeReaderOnly();

            var oggBytes = OggFile.CreateFromWemBytes(wemBytes).WriteData();
            _memoryStream = new MemoryStream(oggBytes, writable: false);
            _waveFileReader = new VorbisWaveReader(_memoryStream);
            _playbackProvider = _waveFileReader;

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

                try
                {
                    _wavePlayer.Init(_playbackProvider);
                }
                catch
                {
                    // Don't leave a half-initialised device behind for the next file
                    _wavePlayer.Dispose();
                    _wavePlayer = null;
                    throw;
                }
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

        // The output devices only take plain 16-bit PCM reliably. WAVs written with a WAVE_FORMAT_EXTENSIBLE
        // header (common for 24-bit and multichannel exports) are re-described as plain PCM or float, and
        // anything that isn't 16-bit PCM is converted to it.
        internal static IWaveProvider CreatePlaybackProvider(WaveFileReader reader, out WaveStream compatibleWaveStream)
        {
            ArgumentNullException.ThrowIfNull(reader);

            compatibleWaveStream = null;
            var format = reader.WaveFormat;
            if (format.Encoding == WaveFormatEncoding.Extensible)
            {
                if (format is not WaveFormatExtraData extensibleFormat || extensibleFormat.ExtraSize < 22)
                    throw new InvalidDataException("The WAV file's extensible format header is incomplete.");

                var subFormat = new Guid(extensibleFormat.ExtraData.AsSpan(6, 16));
                if (subFormat == s_pcmSubFormat)
                    format = new WaveFormat(format.SampleRate, format.BitsPerSample, format.Channels);
                else if (subFormat == s_ieeeFloatSubFormat && format.BitsPerSample == 32)
                    format = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
                else
                    throw new NotSupportedException($"The WAV file's sample format ({subFormat}) can't be played.");

                compatibleWaveStream = new RawSourceWaveStream(reader, format);
            }

            var source = compatibleWaveStream ?? reader;
            if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
                return source;

            return new SampleToWaveProvider16(source.ToSampleProvider());
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
            _playbackProvider = null;
            if (_compatibleWaveStream != null)
            {
                _compatibleWaveStream.Dispose();
                _compatibleWaveStream = null;
            }

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
