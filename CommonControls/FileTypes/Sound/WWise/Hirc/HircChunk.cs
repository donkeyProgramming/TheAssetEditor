using System.Collections.Generic;

namespace CommonControls.FileTypes.Sound.WWise.Hirc
{
    public class HircChunk
    {
        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag= "HIRC", ChunkSize = 0};
        public uint NumHircItems { get; set; }

        public List<HircItem> Hircs { get; set; } = new List<HircItem>();
    }
}
