using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise
{
    public class SoundDataBase
    {
        public BkhdHeader Header { get; set; } = new BkhdHeader();
        public HircChunk HircChuck { get; set; } = new HircChunk();
    }
}
