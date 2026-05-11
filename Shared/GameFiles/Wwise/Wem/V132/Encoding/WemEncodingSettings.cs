namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WemEncodingSettings
    {
        public float Quality { get; set; } = 0.4f;
        public bool UseSeekTable { get; set; } = true;
        
        // Loudness Normalisation (Akd Chunk)
        public bool IncludeAkdChunk { get; set; } = true;
        public float TargetLoudnessDb { get; set; } = -23.0f;
        public bool IncludeHdrEnvelopeData { get; set; } = false;
        public int HdrEnvelopeWindowSizeSamples { get; set; } = 1024;
    }
}
