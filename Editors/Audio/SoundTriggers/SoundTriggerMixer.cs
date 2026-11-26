#nullable enable
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Shared.GameFormats.Audio.Containers.Ogg;

namespace Editors.Audio.SoundTriggers
{
    public sealed class SoundTriggerMixer : IDisposable
    {
        public static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        static readonly ILogger s_logger = Logging.Create<SoundTriggerMixer>();

        readonly object _gate = new();
        readonly Dictionary<uint, float[]> _cache = [];
        readonly MixingSampleProvider _mixer = new(Format) { ReadFully = true };
        IWavePlayer? _device;
        bool _disposed;

        public SoundTriggerMixer(bool openDevice = true, int latencyMs = 30)
            : this(openDevice
                ? [() => new WasapiOut(AudioClientShareMode.Shared, true, latencyMs), () => new WaveOutEvent { DesiredLatency = Math.Max(latencyMs, 60), NumberOfBuffers = 3 }]
                : [])
        {
        }

        internal SoundTriggerMixer(IReadOnlyList<Func<IWavePlayer>> devices)
        {
            for (var i = 0; i < devices.Count; i++)
            {
                IWavePlayer? device = null;
                try
                {
                    device = devices[i]();
                    device.Init(_mixer);
                    // Before Play, so a device that fails to start on its own thread is heard.
                    device.PlaybackStopped += (_, e) =>
                    {
                        if (e.Exception != null)
                            DeviceStopped?.Invoke(e.Exception);
                    };
                    device.Play();
                    _device = device;
                    return;
                }
                catch when (i < devices.Count - 1)
                {
                    device?.Dispose();
                }
                catch
                {
                    device?.Dispose();
                    throw;
                }
            }
        }

        public event Action<Exception>? DeviceStopped;

        public ISampleProvider Output => _mixer;

        public bool Prepare(uint sourceId, Func<byte[]?> wemBytes)
        {
            lock (_gate)
            {
                if (_disposed)
                    return false;
                if (_cache.ContainsKey(sourceId))
                    return true;
            }

            var pcm = Decode(wemBytes());
            if (pcm == null)
                return false;

            lock (_gate)
            {
                if (_disposed)
                    return false;
                _cache[sourceId] = pcm;
            }
            return true;
        }

        public void Play(uint sourceId)
        {
            lock (_gate)
            {
                if (!_cache.TryGetValue(sourceId, out var pcm))
                    return;

                try
                {
                    _mixer.AddMixerInput(new Voice(pcm));
                }
                catch (InvalidOperationException)
                {
                    // NAudio's mixer takes 1,024 inputs at most; the extra voice is dropped.
                }
            }
        }

        public void StopAll()
        {
            lock (_gate)
                _mixer.RemoveAllMixerInputs();
        }

        public void Keep(IEnumerable<uint> sourceIds)
        {
            var keep = sourceIds.ToHashSet();
            lock (_gate)
            {
                foreach (var id in _cache.Keys.Where(id => !keep.Contains(id)).ToList())
                    _cache.Remove(id);
            }
        }

        public void Dispose()
        {
            IWavePlayer? device;
            lock (_gate)
            {
                _disposed = true;
                _mixer.RemoveAllMixerInputs();
                _cache.Clear();
                device = _device;
                _device = null;
            }

            // Outside the lock: WasapiOut's Stop joins its thread.
            if (device == null)
                return;
            foreach (var step in new Action[] { device.Stop, device.Dispose })
            {
                try
                {
                    step();
                }
                catch (Exception e)
                {
                    s_logger.Here().Warning($"SuperView sound: the output device failed to close: {e.Message}");
                }
            }
        }

        static float[]? Decode(byte[]? wem)
        {
            if (wem == null)
                return null;

            try
            {
                var ogg = OggFile.CreateFromWemBytes(wem).WriteData();
                using var reader = new VorbisWaveReader(new MemoryStream(ogg, writable: false));

                ISampleProvider source = reader;
                if (source.WaveFormat.Channels == 1)
                    source = new MonoToStereoSampleProvider(source);
                else if (source.WaveFormat.Channels > 2)
                {
                    var firstTwo = new MultiplexingSampleProvider([source], 2);
                    firstTwo.ConnectInputToOutput(0, 0);
                    firstTwo.ConnectInputToOutput(1, 1);
                    source = firstTwo;
                }
                if (source.WaveFormat.SampleRate != Format.SampleRate)
                    source = new WdlResamplingSampleProvider(source, Format.SampleRate);

                var samples = new List<float>();
                var buffer = new float[Format.SampleRate * Format.Channels];
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                    samples.AddRange(new ArraySegment<float>(buffer, 0, read));
                return samples.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        sealed class Voice(float[] pcm) : ISampleProvider
        {
            int _position;

            public WaveFormat WaveFormat => Format;

            public int Read(float[] buffer, int offset, int count)
            {
                var n = Math.Min(count, pcm.Length - _position);
                Array.Copy(pcm, _position, buffer, offset, n);
                _position += n;
                return n;
            }
        }
    }
}
