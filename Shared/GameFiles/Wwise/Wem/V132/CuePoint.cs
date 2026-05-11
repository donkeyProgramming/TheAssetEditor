using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class CuePoint
    {
        public uint Id { get; set; }
        public uint Position { get; set; }
        public uint DataChunkId { get; set; }
        public uint ChunkStart { get; set; }
        public uint BlockStart { get; set; }
        public uint SampleOffset { get; set; }

        public static CuePoint ReadData(ByteChunk chunk) => new()
        {
            Id = chunk.ReadUInt32(),
            Position = chunk.ReadUInt32(),
            DataChunkId = chunk.ReadUInt32(),
            ChunkStart = chunk.ReadUInt32(),
            BlockStart = chunk.ReadUInt32(),
            SampleOffset = chunk.ReadUInt32(),
        };

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.UInt32.EncodeValue(Id, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(Position, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(DataChunkId, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(ChunkStart, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(BlockStart, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SampleOffset, out _));
            return stream.ToArray();
        }
    }
}
