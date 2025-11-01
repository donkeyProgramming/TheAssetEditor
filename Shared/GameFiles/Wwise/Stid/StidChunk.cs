using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Stid
{
    public class StidChunk
    {
        public static ChunkHeader ReadData(string fileName, ByteChunk chunk)
        {
            var chunkHeader = ChunkHeader.ReadData(chunk);
            chunk.Index += (int)chunkHeader.ChunkSize;
            return chunkHeader;
        }
    }
}
