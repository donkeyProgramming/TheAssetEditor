using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class MusicNodeParams_V136
    {
        public byte Flags { get; set; }
        public NodeBaseParams_V136 NodeBaseParams { get; set; } = new NodeBaseParams_V136();
        public Children_V136 Children { get; set; } = new Children_V136();
        public AkMeterInfo_V136 AkMeterInfo { get; set; } = new AkMeterInfo_V136();
        public byte MeterInfoFlag { get; set; }
        public uint NumStingers { get; set; }
        public List<CAkStinger_V136> StingersList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            Flags = chunk.ReadByte();
            NodeBaseParams.ReadData(chunk);
            Children.ReadData(chunk);
            AkMeterInfo.ReadData(chunk);
            MeterInfoFlag = chunk.ReadByte();
            NumStingers = chunk.ReadUInt32();
            for (var i = 0; i < NumStingers; i++)
                StingersList.Add(CAkStinger_V136.ReadData(chunk));
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(Flags, out _));
            memStream.Write(NodeBaseParams.WriteData());
            memStream.Write(Children.WriteData());
            memStream.Write(AkMeterInfo.WriteData());
            memStream.Write(ByteParsers.Byte.EncodeValue(MeterInfoFlag, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)StingersList.Count, out _));
            foreach (var stinger in StingersList)
                memStream.Write(stinger.WriteData());
            return memStream.ToArray();
        }

        public uint GetSize() =>
            1 // Flags
            + NodeBaseParams.GetSize()
            + Children.GetSize()
            + AkMeterInfo_V136.Size
            + 1 // MeterInfoFlag
            + 4 // NumStingers
            + (uint)StingersList.Count * CAkStinger_V136.Size;

        public class AkMeterInfo_V136
        {
            public double GridPeriod { get; set; }
            public double GridOffset { get; set; }
            public float Tempo { get; set; }
            public byte TimeSigNumBeatsBar { get; set; }
            public byte TimeSigBeatValue { get; set; }

            // 8 + 8 + 4 + 1 + 1
            public const uint Size = 22;

            // The grid fields are IEEE doubles on disk. ByteParsing has no double parser, so the
            // bits are moved through an int64 rather than converted numerically.
            public void ReadData(ByteChunk chunk)
            {
                GridPeriod = BitConverter.Int64BitsToDouble(chunk.ReadInt64());
                GridOffset = BitConverter.Int64BitsToDouble(chunk.ReadInt64());
                Tempo = chunk.ReadSingle();
                TimeSigNumBeatsBar = chunk.ReadByte();
                TimeSigBeatValue = chunk.ReadByte();
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(GridPeriod), out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(GridOffset), out _));
                memStream.Write(ByteParsers.Single.EncodeValue(Tempo, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(TimeSigNumBeatsBar, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(TimeSigBeatValue, out _));
                return memStream.ToArray();
            }
        }

        public class CAkStinger_V136
        {
            public uint TriggerId { get; set; }
            public uint SegmentId { get; set; }
            public uint SyncPlayAt { get; set; }
            public uint CueFilterHash { get; set; }
            public int DontRepeatTime { get; set; }
            public uint NumSegmentLookAhead { get; set; }

            public const uint Size = 24;

            public static CAkStinger_V136 ReadData(ByteChunk chunk)
            {
                return new CAkStinger_V136
                {
                    TriggerId = chunk.ReadUInt32(),
                    SegmentId = chunk.ReadUInt32(),
                    SyncPlayAt = chunk.ReadUInt32(),
                    CueFilterHash = chunk.ReadUInt32(),
                    DontRepeatTime = chunk.ReadInt32(),
                    NumSegmentLookAhead = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(TriggerId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(SegmentId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(SyncPlayAt, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(CueFilterHash, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(DontRepeatTime, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(NumSegmentLookAhead, out _));
                return memStream.ToArray();
            }
        }
    }
}
