using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Stid
{
    public class StidParser
    {
        public static BnkChunkHeader Parse(string fileName, ByteChunk chunk)
        {
            var chunkHeader = BnkChunkHeader.ReadData(chunk);
            chunk.Index += (int)chunkHeader.ChunkSize;
            return chunkHeader;
        }
    }
}
