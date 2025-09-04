using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Didx
{
    public class DidxParser
    {
        public static DidxChunk Parse(string fileName, ByteChunk chunk)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();
            var numItems = chunkSize / DidxChunk.MediaHeader.ByteSize;
            var mediaList = Enumerable.Range(0, (int)numItems)
                .Select(x => new DidxChunk.MediaHeader(chunk))
                .ToList();
            return new DidxChunk { MediaList = mediaList };
        }
    }
}
