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
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.Byte.EncodeValue(PacketType, out _));
            stream.Write(System.Text.Encoding.ASCII.GetBytes(HeaderTag));

            var vendorBytes = System.Text.Encoding.ASCII.GetBytes(VendorString);
            stream.Write(ByteParsers.UInt32.EncodeValue((uint)vendorBytes.Length, out _));
            stream.Write(vendorBytes);

            stream.Write(ByteParsers.UInt32.EncodeValue((uint)UserComments.Count, out _));
            foreach (var comment in UserComments)
            {
                var commentBytes = System.Text.Encoding.ASCII.GetBytes(comment);
                stream.Write(ByteParsers.UInt32.EncodeValue((uint)commentBytes.Length, out _));
                stream.Write(commentBytes);
            }

            var framingByte = (byte)BitHelper.ExtractBits(FramingBit, 0, 1);
            stream.Write(ByteParsers.Byte.EncodeValue(framingByte, out _));

            return stream.ToArray();
        }
    }
}
