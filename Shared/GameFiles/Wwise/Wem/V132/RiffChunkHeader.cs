using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class RiffChunkHeader(string tag, uint chunkSize)
    {
        public const uint HeaderSize = 8;
        public const int ChunkPaddingAlignment = 2;

        public string Tag { get; set; } = tag;
        public uint ChunkSize { get; set; } = chunkSize;

        public static RiffChunkHeader ReadData(ByteChunk chunk)
        {
            var tag = System.Text.Encoding.ASCII.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();
            return new RiffChunkHeader(tag, chunkSize);
        }

        public static RiffChunkHeader PeekFromBytes(ByteChunk chunk)
        {
            var peekBytes = chunk.PeekChunk((int)HeaderSize);
            return ReadData(peekBytes);
        }

        public static byte[] WriteData(RiffChunkHeader header)
        {
            if (header.Tag.Length != 4)
                throw new Exception($"Header not valid {header.Tag}");

            using var stream = new MemoryStream();
            stream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[0], out _));
            stream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[1], out _));
            stream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[2], out _));
            stream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[3], out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(header.ChunkSize, out _));

            var byteArray = stream.ToArray();
            return byteArray;
        }

    }
}
