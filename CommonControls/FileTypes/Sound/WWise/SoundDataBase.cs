using CommonControls.FileTypes.Sound.WWise.Bkhd;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using System.Collections.Generic;

namespace CommonControls.FileTypes.Sound.WWise
{
    public class SoundDataBase
    {
        public BkhdHeader Header { get; set; } = new BkhdHeader();
        public HircChunk HircChuck { get; set; } = new HircChunk();
    }
}
