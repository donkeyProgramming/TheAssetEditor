using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class JunkChunk : RiffChunk
    {
        public byte[] PaddingBytes { get; set; } = [];

        public JunkChunk()
        {
            Tag = WemChunks.Junk;
        }

        public override void ReadData(ByteChunk chunk)
        {
            PaddingBytes = chunk.ReadBytes(chunk.BytesLeft);
        }

        public override byte[] WriteData() => throw new NotImplementedException("Writing is not supported.");
    }
}
