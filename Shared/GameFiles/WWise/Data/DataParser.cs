using Shared.Core.ByteParsing;
using System.Text;

namespace Shared.GameFormats.WWise.Data
{
    public class DataParser
    {
        public ByteChunk Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();

            var buffer = chunk.CreateSub((int)chunkSize);
            return buffer;
        }
    }
}
