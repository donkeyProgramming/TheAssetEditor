using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class FxChunk_V112
    {
        public byte FxIndex { get; set; }
        public uint FxId { get; set; }
        public byte IsShareSet { get; set; }
        public byte IsRendered { get; set; }

        public void ReadData(ByteChunk chunk)
        {
            FxIndex = chunk.ReadByte();
            FxId = chunk.ReadUInt32();
            IsShareSet = chunk.ReadByte();
            IsRendered = chunk.ReadByte();
        }
    }
}
