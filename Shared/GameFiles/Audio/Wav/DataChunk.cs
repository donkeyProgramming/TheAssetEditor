using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Wav
{
    public class DataChunk : RiffChunk
    {
        public const string ChunkTag = "data";
        public byte[] Data { get; set; } = [];

        public DataChunk()
        {
            Tag = ChunkTag;
        }

        public override void ReadData(ByteChunk chunk)
        {
            Data = chunk.ReadBytes(chunk.BytesLeft);
        }

        public override byte[] WriteData()
        {
            return Data;
        }
    }
}
