using Shared.Core.ByteParsing;

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

        public class AkMeterInfo_V136
        {
            public double GridPeriod { get; set; }
            public double GridOffset { get; set; }
            public float Tempo { get; set; }
            public byte TimeSigNumBeatsBar { get; set; }
            public byte TimeSigBeatValue { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                GridPeriod = chunk.ReadInt64();
                GridOffset = chunk.ReadInt64();
                Tempo = chunk.ReadSingle();
                TimeSigNumBeatsBar = chunk.ReadByte();
                TimeSigBeatValue = chunk.ReadByte();
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
        }
    }
}
