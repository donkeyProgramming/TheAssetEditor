using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    // The JUNK chunk contains padding bytes used for chunk / data alignment.
    public class JunkChunk : RiffChunk
    {
        public const string ChunkTag = "JUNK";

        public byte[] Padding { get; set; } = [];

        public JunkChunk()
        {
            Tag = ChunkTag;
        }

        public override void ReadData(ByteChunk chunk)
        {
            Padding = chunk.ReadBytes(chunk.BytesLeft);
        }

        public override byte[] WriteData() => Padding;

        public static byte[] CalculatePadding(int targetDataChunkOffsetBytes, int bytesBeforeJunk, int bytesAfterJunk)
        {
            if (targetDataChunkOffsetBytes <= 0)
                return [];

            var junkSize = targetDataChunkOffsetBytes - bytesBeforeJunk - bytesAfterJunk;
            return junkSize > 0 ? new byte[junkSize] : [];
        }
    }
}
