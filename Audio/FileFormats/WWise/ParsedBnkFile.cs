using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Didx;
using Audio.FileFormats.WWise.Hirc;
using Shared.Core.ByteParsing;

namespace Audio.FileFormats.WWise
{
    public class ParsedBnkFile
    {
        public BkhdHeader Header { get; internal set; } = new BkhdHeader();
        public HircChunk HircChuck { get; internal set; } = new HircChunk();
        public DidxChunk DidxChunk { get; internal set; }
        public ByteChunk DataChunk { get; internal set; }
    }
}
