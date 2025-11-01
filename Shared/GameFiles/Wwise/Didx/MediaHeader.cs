using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Didx
{
    public class MediaHeader
    {
        public static uint ByteSize => 12;
        public uint Id { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }

        public static MediaHeader ReadData(ByteChunk chunk)
        {
            return new MediaHeader
            {
                Id = chunk.ReadUInt32(),
                Offset = chunk.ReadUInt32(),
                Size = chunk.ReadUInt32()
            };
        }
    }
}
