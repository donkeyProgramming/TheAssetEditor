using Filetypes.ByteParsing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Audio.FileFormats.WWise.Didx
{
    public class DidxParser : IParser
    {
        public List<MediaHeader> MediaList{get;set;} = new List<MediaHeader>();

        public void Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();

            MediaList = Enumerable.Range(0, (int)chunkSize)
                .Select(x => new MediaHeader(chunk))
                .ToList();
        }

        public class MediaHeader
        {
            public uint Id { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }

            public MediaHeader(ByteChunk chunk)
            {
                Id = chunk.ReadUInt32();
                Offset = chunk.ReadUInt32();
                Size = chunk.ReadUInt32();
            }
        }
    }
}
