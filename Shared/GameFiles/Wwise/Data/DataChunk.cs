using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Data
{
    public class DataChunk
    {
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public ByteChunk Data { get; set; } = new ByteChunk([]);

        public static DataChunk ReadData(string fileName, ByteChunk chunk)
        {
            var dataChunk = new DataChunk { ChunkHeader = ChunkHeader.ReadData(chunk) };
            dataChunk.Data = chunk.CreateSub((int)dataChunk.ChunkHeader.ChunkSize);
            return dataChunk;
        }
    }
}
