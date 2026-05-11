using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Wav
{
    public class WavChunkHeader
    {
        public const int HeaderSize = 8;
        public const int ChunkPaddingAlignment = 2;

        public string Tag { get; set; } = string.Empty;
        public int ChunkSize { get; set; }

        public static WavChunkHeader ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < HeaderSize)
                throw new InvalidDataException($"WAV chunk header must be at least {HeaderSize} bytes.");

            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var size = chunk.ReadInt32();

            if (size < 0 || size > chunk.BytesLeft)
                throw new InvalidDataException("WAV chunk extends beyond the end of the file.");

            return new WavChunkHeader
            {
                Tag = tag,
                ChunkSize = size,
            };
        }

        public static void WriteData(Stream stream, WavChunkHeader header)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(header);

            if (header.Tag.Length != 4)
                throw new InvalidDataException($"WAV chunk tag must be 4 bytes, got '{header.Tag}'.");

            stream.Write(Encoding.ASCII.GetBytes(header.Tag));
            stream.Write(ByteParsers.Int32.EncodeValue(header.ChunkSize, out _));
        }
    }
}
