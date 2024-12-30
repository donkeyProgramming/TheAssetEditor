namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircChunk
    {
        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = BankChunkTypes.HIRC, ChunkSize = 0 };
        public uint NumHircItems { get; set; }
        public List<HircItem> HircItems { get; set; } = [];

        public void SetFromHircList(List<HircItem> hircList)
        {
            HircItems.AddRange(hircList);
            ChunkHeader.ChunkSize = (uint)(hircList.Sum(x => x.SectionSize) + hircList.Count * 5 + 4);
            NumHircItems = (uint)hircList.Count;
        }
    }
}
