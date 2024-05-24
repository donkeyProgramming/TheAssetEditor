using Shared.Core.ByteParsing;

namespace Audio.FileFormats.WWise.Stid
{
    public class StidParser
    {
        public BnkChunkHeader Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var chunckHeader = BnkChunkHeader.CreateFromBytes(chunk);
            chunk.Index += (int)chunckHeader.ChunkSize;
            return chunckHeader;
        }
    }


}
