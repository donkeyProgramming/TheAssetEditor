using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Stid
{
    public class StidChunk
    {
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public ByteChunk Data { get; set; } = new ByteChunk([]);

        public static StidChunk ReadData(string fileName, ByteChunk chunk)
        {
            var stidChunk = new StidChunk { ChunkHeader = ChunkHeader.ReadData(chunk) };
            stidChunk.Data = chunk.CreateSub((int)stidChunk.ChunkHeader.ChunkSize);
            return stidChunk;
        }
    }
}
