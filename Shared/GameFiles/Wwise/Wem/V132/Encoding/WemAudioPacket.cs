using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WemAudioPacket
    {
        public const int LengthPrefixSize = sizeof(ushort);

        public byte[] Data { get; set; } = [];
        public long GranulePosition { get; set; }

        public static WemAudioPacket ReadData(ByteChunk chunk)
        {
            var packet = new WemAudioPacket();
            var dataSize = chunk.ReadUShort();
            packet.Data = chunk.ReadBytes(dataSize);
            return packet;
        }

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.UShort.EncodeValue((ushort)Data.Length, out _));
            stream.Write(Data);
            return stream.ToArray();
        }
    }
}
