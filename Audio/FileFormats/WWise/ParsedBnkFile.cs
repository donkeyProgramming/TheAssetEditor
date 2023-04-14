using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;

namespace Audio.FileFormats.WWise
{
    public class ParsedBnkFile
    {
        public BkhdHeader Header { get; set; } = new BkhdHeader();
        public HircChunk HircChuck { get; set; } = new HircChunk();
    }
}
