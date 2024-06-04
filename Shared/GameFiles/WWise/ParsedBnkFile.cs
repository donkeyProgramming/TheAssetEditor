using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise.Bkhd;
using Shared.GameFormats.WWise.Didx;
using Shared.GameFormats.WWise.Hirc;

namespace Shared.GameFormats.WWise
{
    public class ParsedBnkFile
    {
        public BkhdHeader Header { get; internal set; } = new BkhdHeader();
        public HircChunk HircChuck { get; internal set; } = new HircChunk();
        public DidxChunk DidxChunk { get; internal set; }
        public ByteChunk DataChunk { get; internal set; }
    }
}
