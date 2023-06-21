using Filetypes.ByteParsing;
using System.Text;

namespace Audio.FileFormats.WWise.Data
{
    public class DataParser : IParser
    {
        public ByteChunk Buffer { get; set; }

        public void Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();

            Buffer = chunk.CreateSub((int)chunkSize);
        }
    }
}
