using Shared.ByteParsing;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    // The data chunk contains seek / setup data and encoded audio packets.
    public class DataChunk : RiffChunk
    {
        public const string ChunkTag = "data";
        public const int SetupPacketLengthPrefixSize = 2;

        public List<WemSeekTableRecord> SeekTable { get; set; } = [];
        public byte[] SetupPacket { get; set; } = [];
        public List<WemAudioPacket> AudioPackets { get; set; } = [];
        public FmtChunk? FmtChunk { get; set; }

        public DataChunk()
        {
            Tag = ChunkTag;
        }

        public override void ReadData(ByteChunk chunk)
        {
            if (FmtChunk == null)
                throw new InvalidOperationException("Set FmtChunk before calling ReadData.");

            var recordCount = FmtChunk.SeekTableSize / WemSeekTableRecord.Size;
            for (var i = 0; i < recordCount; i++)
                SeekTable.Add(WemSeekTableRecord.ReadData(chunk));

            var setupPacketSize = chunk.ReadUShort();
            SetupPacket = chunk.ReadBytes(setupPacketSize);

            var bytesConsumed = (int)FmtChunk.SeekTableSize + SetupPacketLengthPrefixSize + setupPacketSize;
            var audioStart = (int)FmtChunk.FirstAudioPacketOffset;
            if (audioStart > bytesConsumed)
                chunk.Advance(audioStart - bytesConsumed);

            while (chunk.BytesLeft >= WemAudioPacket.LengthPrefixSize)
                AudioPackets.Add(WemAudioPacket.ReadData(chunk));
        }

        public override byte[] WriteData()
        {
            using var stream = new MemoryStream();

            foreach (var record in SeekTable)
                stream.Write(record.WriteData());

            stream.Write(ByteParsers.UShort.EncodeValue((ushort)SetupPacket.Length, out _));
            stream.Write(SetupPacket);

            foreach (var packet in AudioPackets)
                stream.Write(packet.WriteData());

            var byteArray = stream.ToArray();
            if (FmtChunk != null)
            {
                var sanityReload = new DataChunk { FmtChunk = FmtChunk };
                sanityReload.ReadChunk(new ByteChunk(byteArray));
            }

            return byteArray;
        }
    }
}
