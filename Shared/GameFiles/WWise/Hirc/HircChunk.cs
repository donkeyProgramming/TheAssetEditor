namespace Shared.GameFormats.WWise.Hirc
{
    public class HircChunk
    {
        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = "HIRC", ChunkSize = 0 };
        public uint NumHircItems { get; set; }
        public List<HircItem> Hircs { get; set; } = [];

        public void SetFromHircList(List<HircItem> hircList)
        {
            Hircs.AddRange(hircList);
            ChunkHeader.ChunkSize = (uint)(hircList.Sum(x => x.Size) + hircList.Count() * 5 + 4);
            NumHircItems = (uint)hircList.Count;
        }
    }
}
