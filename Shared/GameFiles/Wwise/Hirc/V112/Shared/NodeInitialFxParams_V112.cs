using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class NodeInitialFxParams_V112
    {
        public byte IsOverrideParentFx { get; set; }
        public byte NumFx { get; set; }
        public byte BitsFxBypass { get; set; }
        public List<FxChunk_V112> FxList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            IsOverrideParentFx = chunk.ReadByte();
            NumFx = chunk.ReadByte();
            if (NumFx != 0)
            {
                BitsFxBypass = chunk.ReadByte();
                for (var i = 0; i < NumFx; i++)
                {
                    var fxChunk = new FxChunk_V112();
                    fxChunk.ReadData(chunk);
                    FxList.Add(fxChunk);
                }
            }
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(IsOverrideParentFx, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BitsFxBypass, out _));

            if (NumFx != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var isOverrideParentFxSize = ByteHelper.GetPropertyTypeSize(IsOverrideParentFx);
            var numFxSize = ByteHelper.GetPropertyTypeSize(NumFx);

            if (NumFx != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            return isOverrideParentFxSize + numFxSize;
        }
    }
}
