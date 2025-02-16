using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Didx
{
    public class DidxChunk
    {
        public List<MediaHeader> MediaList { get; set; } = [];

        public class MediaHeader
        {
            public static uint ByteSize => 12;
            public uint ID { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }

            public MediaHeader(ByteChunk chunk)
            {
                ID = chunk.ReadUInt32();
                Offset = chunk.ReadUInt32();
                Size = chunk.ReadUInt32();
            }
        }
    }
}
