using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    /// <summary>
    /// Wwise "akd " RIFF chunk — pre-computed analysis data baked into a WEM during the conversion/analysis phase.
    ///
    /// Purpose:
    /// The chunk stores per-file loudness and HDR envelope data so the Wwise runtime can apply normalisation
    /// and dynamic gain without re-analysing the audio on load.  It feeds two independent Wwise features:
    ///
    ///   1. Source Loudness Normalisation (Sound Properties -> "Enable Loudness Normalisation" checkbox).
    ///      When enabled, the runtime multiplies playback gain by LoudnessNormalisationGain so the sound
    ///      hits a target loudness level (default -23 LUFS per ITU-R BS.1770).  Wwise documentation states
    ///      the formula as:  gain_dB = target_dB − measured_dB.  The value stored here is the equivalent
    ///      linear amplitude multiplier:  gain = 10^(gain_dB / 20).
    ///      The normalisation gain is intentionally transparent to Wwise's voice pipeline — it does not
    ///      affect virtual-voice threshold evaluation, HDR attenuation, or the Voice Monitor.
    ///
    ///   2. HDR Bus processing (High Dynamic Range).
    ///      HdrPeakRms and the optional per-block envelope data let the HDR bus track the instantaneous
    ///      loudness of a playing sound over time and feed it back into the HDR gain computer, enabling
    ///      the bus to attenuate competing quiet sounds when a loud sound plays.
    ///
    /// Generation:
    /// Wwise generates this chunk during WAV→WEM conversion when AnalysisTypes ≥ 1 in the .wsources XML:
    ///   AnalysisTypes="1" - HDR analysis only  (LoudnessNormalisationGain = 1.0, no loudness correction)
    ///   AnalysisTypes="2" - Loudness analysis (BS.1770 K-weighted integrated loudness with gating)
    ///   AnalysisTypes="3" - Both HDR and loudness analysis
    ///   AnalysisTypes="0" - No akd chunk emitted at all
    /// The IncludeEnvelope attribute in .wsources controls whether the variable envelope tail is written.
    ///
    /// Reverse Engineering:
    /// Layout and field names were proven by disassembling WwiseSoundEngine.dll (Wwise 2019.2.15.7667)
    /// using the Python 'capstone' disassembly library against the PE export table via 'pefile':
    ///
    ///   • WriteAnalysisChunk (IPluginHostConversionHelpers) — the function that serialises this chunk.
    ///     Disassembly showed four sequential movss/mov writes: float, float, dword, float — confirming
    ///     the layout and that field 3 is uint32 (not float as the zero-filled corpus initially suggested).
    ///
    ///   • WavLoudnessAnalysis / CalculateLoudnessNormalisationGain — string literals found by scanning
    ///     the .rdata section of WwiseSoundEngine.dll, confirming the name of field 1.
    ///
    ///   • GetDownmixNormalisationGain — function name found in the same string scan, confirming field 2.
    ///
    ///   • WavHdrAnalysis::GetPeakRMS() — string found in WwiseSoundEngine.dll; disassembly of the
    ///     function body showed 'movss xmm0, [rcx+0x50]; ret', confirming it returns a single float
    ///     (HdrPeakRms, field 4) read from a fixed struct offset.
    ///
    ///   • AkFileParser::EnvelopePoint / SourceContext::SetEnvelopePoint — string and disassembly proving
    ///     the 6-byte envelope record layout: uint32 sample-index + uint16 quantized-dB amplitude.
    ///
    /// Loudness value format validated empirically: our BS.1770 encoder produces LoudnessNormalisationGain
    /// = 0.358357 for harmony_of_shadows.wav; Wwise produces 0.358360 — a difference of 0.000002,
    /// confirming the value is a linear amplitude multiplier (not a dB scalar).
    /// </summary>
    public class AkdChunk : RiffChunk
    {
        private const uint FixedPayloadSize = 16;
        public const uint EnvelopePointSize = 6;

        /// <summary>
        /// Linear amplitude multiplier applied at runtime when "Enable Loudness Normalisation" is active on the Sound.
        /// Computed as: gain = 10^((targetLoudness − measuredLoudness) / 20), where loudness is measured
        /// using ITU-R BS.1770 K-weighted integrated loudness with absolute (−70 LUFS) and relative (−10 LU) gating.
        /// Default Wwise target is −23 LUFS (ITU-R BS.1770 / EBU R128).
        /// Name confirmed from WavLoudnessAnalysis / CalculateLoudnessNormalisationGain strings in WwiseSoundEngine.dll.
        /// </summary>
        public float LoudnessNormalisationGain { get; set; }

        /// <summary>
        /// Linear amplitude multiplier that corrects for level change when downmixing multichannel audio to fewer channels.
        /// For mono and stereo sources this is always 1.0. For N-channel surround: sqrt(2 / N).
        /// Name confirmed from GetDownmixNormalisationGain string in WwiseSoundEngine.dll.
        /// </summary>
        public float DownmixNormalisationGain { get; set; } = 1.0f;

        /// <summary>
        /// Number of HDR envelope point records in the variable-length tail (6 bytes each).
        /// Zero when the file was converted without IncludeEnvelope or without HDR analysis.
        /// </summary>
        public uint HdrEnvelopePointCount { get; set; }

        /// <summary>
        /// Peak RMS value of the HDR loudness envelope across the whole file, used by the Wwise HDR bus.
        /// Name and type confirmed by disassembling WavHdrAnalysis::GetPeakRMS() in WwiseSoundEngine.dll:
        /// the function body is `movss xmm0, [rcx+0x50]; ret`, returning a single float from a fixed struct offset.
        /// Zero when no HDR analysis was performed (AnalysisTypes &lt; 1 or IncludeEnvelope not set).
        /// </summary>
        public float HdrPeakRms { get; set; }

        /// <summary>
        /// Raw bytes of packed AkFileParser::EnvelopePoint records that follow the fixed 16-byte header.
        /// Each record is 6 bytes: uint32 sample-index + uint16 quantized-dB amplitude.
        /// Layout proven from SourceContext::SetEnvelopePoint / SetEnvelopePoints disassembly in WwiseSoundEngine.dll.
        /// Empty (length 0) when HdrEnvelopePointCount is zero.
        /// </summary>
        public byte[] EnvelopeData { get; set; } = [];

        public AkdChunk()
        {
            Tag = WemChunks.Akd;
        }

        public override void ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < FixedPayloadSize)
                throw new InvalidDataException($"WEM akd chunk must be at least {FixedPayloadSize} bytes, got {chunk.BytesLeft}.");

            LoudnessNormalisationGain = chunk.ReadSingle();
            DownmixNormalisationGain = chunk.ReadSingle();
            HdrEnvelopePointCount = chunk.ReadUInt32();
            HdrPeakRms = chunk.ReadSingle();

            if (chunk.BytesLeft > 0)
                EnvelopeData = chunk.ReadBytes(chunk.BytesLeft);

            var expectedEnvelopeByteCount = checked((int)(HdrEnvelopePointCount * EnvelopePointSize));
            if (EnvelopeData.Length != expectedEnvelopeByteCount)
                throw new InvalidDataException($"WEM akd chunk envelope byte count mismatch. Header declares {HdrEnvelopePointCount} points ({expectedEnvelopeByteCount} bytes), but chunk contains {EnvelopeData.Length} bytes.");
        }

        public override byte[] WriteData()
        {
            if (EnvelopeData.Length % EnvelopePointSize != 0)
                throw new InvalidDataException($"WEM akd chunk envelope data length must be a multiple of {EnvelopePointSize} bytes.");

            var derivedEnvelopePointCount = (uint)(EnvelopeData.Length / EnvelopePointSize);
            var envelopePointCount = HdrEnvelopePointCount;
            
            if (envelopePointCount == 0 && derivedEnvelopePointCount > 0)
                envelopePointCount = derivedEnvelopePointCount;
            else if (envelopePointCount != derivedEnvelopePointCount)
                throw new InvalidDataException($"WEM akd chunk envelope point count mismatch. Header declares {HdrEnvelopePointCount} points but payload encodes {derivedEnvelopePointCount}.");

            var totalSize = FixedPayloadSize + (uint)EnvelopeData.Length;
            using var stream = new MemoryStream((int)totalSize);

            stream.Write(BitConverter.GetBytes(LoudnessNormalisationGain));
            stream.Write(BitConverter.GetBytes(DownmixNormalisationGain));
            stream.Write(BitConverter.GetBytes(envelopePointCount));
            stream.Write(BitConverter.GetBytes(HdrPeakRms));

            if (EnvelopeData.Length > 0)
                stream.Write(EnvelopeData);

            var byteArray = stream.ToArray();

            var sanityReload = new AkdChunk();
            sanityReload.ReadChunk(new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
