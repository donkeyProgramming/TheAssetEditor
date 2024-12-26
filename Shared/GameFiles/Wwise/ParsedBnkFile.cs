using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Shared.GameFormats.Wwise
{
    public class ParsedBnkFile
    {
        public BkhdHeader Header { get; internal set; } = new BkhdHeader();
        public HircChunk HircChuck { get; internal set; } = new HircChunk();
        public DidxChunk DidxChunk { get; internal set; }
        public ByteChunk DataChunk { get; internal set; }
    }
}
