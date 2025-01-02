using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise
{
    public class BnkChunkHeader
    {
        public static uint HeaderByteSize { get => 8; }
        public string Tag { get; set; }
        public uint ChunkSize { get; set; }

        public static BnkChunkHeader ReadData(ByteChunk chunk)
        {
            var instance = new BnkChunkHeader();
            instance.Tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            instance.ChunkSize = chunk.ReadUInt32();
            return instance;
        }

        public static BnkChunkHeader PeekFromBytes(ByteChunk chunk)
        {
            var peakBytes = chunk.PeekChunk(8);
            return ReadData(peakBytes);
        }

        public static byte[] WriteData(BnkChunkHeader header)
        {
            if (header.Tag.Length != 4)
                throw new Exception($"Header not valid {header.Tag}");

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[0], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[1], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[2], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[3], out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.ChunkSize, out _));

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var reload = ReadData(new ByteChunk(byteArray));
            return byteArray;
        }
    }
}
