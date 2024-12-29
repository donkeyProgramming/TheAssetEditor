using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Stid
{
    public class StidParser
    {
        public static BnkChunkHeader Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var chunkHeader = BnkChunkHeader.CreateSpecificData(chunk);
            chunk.Index += (int)chunkHeader.ChunkSize;
            return chunkHeader;
        }
    }
}
