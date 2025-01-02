using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkMediaMap_V136
    {
        public byte Index { get; set; }
        public uint SourceId { get; set; }

        public static AkMediaMap_V136 ReadData(ByteChunk chunk)
        {
            return new AkMediaMap_V136
            {
                Index = chunk.ReadByte(),
                SourceId = chunk.ReadUInt32()
            };
        }
    }
}
