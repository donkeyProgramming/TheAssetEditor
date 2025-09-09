using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class UnknownHircItem : HircItem
    {
        public string ErrorMsg { get; set; }

        protected override void ReadData(ByteChunk chunk)
        {
            chunk.ReadBytes((int)SectionSize - 4);
        }

        public override void UpdateSectionSize() => throw new NotImplementedException();
        public override byte[] WriteData() => throw new NotImplementedException();
    }
}
