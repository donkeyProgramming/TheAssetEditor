using FileTypes.Sound.WWise.Bkhd;
using FileTypes.Sound.WWise.Hirc;
using System.Collections.Generic;

namespace FileTypes.Sound.WWise
{
    public class SoundDataBase
    {
        public BkhdHeader Header { get; set; }
        public List<HricItem> Hircs { get; set; } = new List<HricItem>();
    }
}
