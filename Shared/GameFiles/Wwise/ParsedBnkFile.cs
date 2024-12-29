using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Shared.GameFormats.Wwise
{
    public class ParsedBnkFile
    {
        public BkhdChunk BkhdChunk { get; internal set; } = new BkhdChunk();
        public HircChunk HircChunk { get; internal set; } = new HircChunk();
        public DidxChunk DidxChunk { get; internal set; }
        public ByteChunk DataChunk { get; internal set; }
    }
}
