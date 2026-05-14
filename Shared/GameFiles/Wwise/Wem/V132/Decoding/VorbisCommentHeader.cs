using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132.Decoding
{
    public class VorbisCommentHeader
    {
        public byte PacketType { get; } = 0x03;
        public string HeaderTag { get; } = "vorbis";
        public uint FramingBit { get; } = 1u;
        public string VendorString { get; set; } = "WwiseVorb";
        public List<string> UserComments { get; set; } = [];

        public byte[] WriteData()
        {
            var writer = new BitWriter(128);
            writer.WriteByte(PacketType);
            writer.WriteAscii(HeaderTag);
            writer.WriteBits((uint)VendorString.Length, 32);
            writer.WriteAscii(VendorString);
            writer.WriteBits((uint)UserComments.Count, 32);

            foreach (var comment in UserComments)
            {
                writer.WriteBits((uint)comment.Length, 32);
                writer.WriteAscii(comment);
            }

            writer.WriteBits(FramingBit, 1);
            writer.AlignToByte();

            return writer.ToArray();
        }
    }
}
