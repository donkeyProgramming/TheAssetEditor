using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class UnknownChunk : RiffChunk
    {
        public byte[] Data { get; set; } = [];

        public override void ReadData(ByteChunk chunk)
        {
            Data = chunk.ReadBytes(chunk.BytesLeft);
        }

        public override byte[] WriteData() => throw new NotImplementedException("Writing is not supported.");
    }
}
