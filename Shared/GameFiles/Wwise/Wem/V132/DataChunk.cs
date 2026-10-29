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

        /// <summary>
        /// Bytes after the last complete packet that cannot be one. About one in ten WH3 music wems
        /// end their data chunk a few bytes past the last packet, with a length prefix that would run
        /// past the chunk. Kept so the file writes back unchanged.
        /// </summary>
        public byte[] TrailingBytes { get; set; } = [];
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
            {
                var packetStart = chunk.Index;
                var packetSize = chunk.ReadUShort();
                chunk.Index = packetStart;
                if (packetSize > chunk.BytesLeft - WemAudioPacket.LengthPrefixSize)
                    break;

                AudioPackets.Add(WemAudioPacket.ReadData(chunk));
            }

            TrailingBytes = chunk.ReadBytes(chunk.BytesLeft);
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

            stream.Write(TrailingBytes);

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
