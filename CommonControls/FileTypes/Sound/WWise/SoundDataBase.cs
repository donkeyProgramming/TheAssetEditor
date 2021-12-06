using CommonControls.FileTypes.Sound.WWise.Bkhd;
using System.Collections.Generic;

namespace CommonControls.FileTypes.Sound.WWise
{
    public class SoundDataBase
    {
        public BkhdHeader Header { get; set; }
        public List<HircItem> Hircs { get; set; } = new List<HircItem>();
    }
}
