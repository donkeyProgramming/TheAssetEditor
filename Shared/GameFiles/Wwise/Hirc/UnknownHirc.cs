using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class UnknownHirc : HircItem
    {
        public string ErrorMsg { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            chunk.ReadBytes((int)SectionSize - 4);
        }

        public override void UpdateSectionSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
