using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Didx
{
    public class DidxChunk
    {
        public List<MediaHeader> MediaList { get; set; } = new();

        public class MediaHeader
        {
            public static uint ByteSize => 12;
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

    public class DidxParser
    {
        public DidxChunk Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();

            var numItems = chunkSize / DidxChunk.MediaHeader.ByteSize;
            var MediaListediaList = Enumerable.Range(0, (int)numItems)
                .Select(x => new DidxChunk.MediaHeader(chunk))
                .ToList();

            return new DidxChunk { MediaList = MediaListediaList };
        }


    }
}
