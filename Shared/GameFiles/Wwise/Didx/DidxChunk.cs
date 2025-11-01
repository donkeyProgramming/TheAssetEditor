using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Didx
{
    public partial class DidxChunk
    {
        public List<MediaHeader> MediaList { get; set; } = [];

        public static DidxChunk ReadData(string fileName, ByteChunk chunk)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();
            var numItems = chunkSize / MediaHeader.ByteSize;
            var mediaList = Enumerable.Range(0, (int)numItems)
                .Select(item => MediaHeader.ReadData(chunk))
                .ToList();
            return new DidxChunk { MediaList = mediaList };
        }
    }
}
