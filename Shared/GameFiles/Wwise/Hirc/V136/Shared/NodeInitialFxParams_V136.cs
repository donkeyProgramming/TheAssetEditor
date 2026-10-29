using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class NodeInitialFxParams_V136
    {
        public byte IsOverrideParentFx { get; set; }
        public byte NumFx { get; set; }
        public byte BitsFxBypass { get; set; }
        public List<FxChunk_V136> FxChunk { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            IsOverrideParentFx = chunk.ReadByte();
            NumFx = chunk.ReadByte();
            if (NumFx != 0)
            {
                BitsFxBypass = chunk.ReadByte();
                for (var i = 0; i < NumFx; i++)
                    FxChunk.Add(FxChunk_V136.ReadData(chunk));
            }
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(IsOverrideParentFx, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(NumFx, out _));

            if (NumFx != 0)
            {
                memStream.Write(ByteParsers.Byte.EncodeValue(BitsFxBypass, out _));
                foreach (var fxChunk in FxChunk)
                    memStream.Write(fxChunk.WriteData());
            }

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var isOverrideParentFxSize = ByteHelper.GetPropertyTypeSize(IsOverrideParentFx);
            var numFxSize = ByteHelper.GetPropertyTypeSize(NumFx);

            if (NumFx == 0)
                return isOverrideParentFxSize + numFxSize;

            var bitsFxBypassSize = ByteHelper.GetPropertyTypeSize(BitsFxBypass);
            return isOverrideParentFxSize + numFxSize + bitsFxBypassSize + (uint)FxChunk.Count * FxChunk_V136.Size;
        }
    }
}
