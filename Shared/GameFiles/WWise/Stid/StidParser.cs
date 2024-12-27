using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Stid
{
    public class StidParser
    {
        public static BnkChunkHeader Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var chunckHeader = BnkChunkHeader.CreateFromBytes(chunk);
            chunk.Index += (int)chunckHeader.ChunkSize;
            return chunckHeader;
        }
    }
}
