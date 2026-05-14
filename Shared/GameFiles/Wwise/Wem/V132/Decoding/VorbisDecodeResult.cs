using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Shared.GameFormats.Wwise.Wem.V132.Decoding
{
    public class VorbisDecodeResult
    {
        public byte[] CommentPacket { get; set; } = [];
        public byte[] IdentificationPacket { get; set; } = [];
        public int LargeBlockSize { get; set; }
        public int SmallBlockSize { get; set; }
        public bool UsesWwisePacketHeaderVariant { get; set; }
        public VorbisModeConfiguration ModeConfig { get; set; } = new();
        public byte[] SetupPacket { get; set; } = [];
    }
}
