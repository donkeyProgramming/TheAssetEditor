namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WemVorbisSetupData
    {
        public byte[] SetupPacketBytes { get; set; } = [];
        public VorbisModeConfiguration ModeConfiguration { get; set; } = new();
    }
}
