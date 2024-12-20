using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkMusicSegment_v136 : HircItem, INodeBaseParamsAccessor
    {
        public MusicNodeParams MusicNodeParams { get; set; }
        public double FDuration { get; set; }
        public List<AkMusicMarkerWwise> PArrayMarkersList { get; set; } = [];

        public NodeBaseParams NodeBaseParams => MusicNodeParams.NodeBaseParams;

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            MusicNodeParams = MusicNodeParams.Create(chunk);

            FDuration = chunk.ReadInt64(); //chunk.ReadDouble();

            var ulNumMarkers = chunk.ReadUInt32();
            for (var i = 0; i < ulNumMarkers; i++)
                PArrayMarkersList.Add(AkMusicMarkerWwise.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkMusicMarkerWwise
    {
        public uint Id { get; set; }
        public double FPosition { get; set; }

        //see below
        //public string pMarkerName { get; set; }
        public List<byte> PMarkerName { get; set; } = [];

        public static AkMusicMarkerWwise Create(ByteChunk chunk)
        {
            var instance = new AkMusicMarkerWwise();
            instance.Id = chunk.ReadUInt32();
            instance.FPosition = chunk.ReadInt64(); //chunk.ReadDouble();

            //instance.pMarkerName = chunk.ReadString();
            //The above wasn't working because uStringSize is an uint32, yet the ReadString was trying to read it as a uint16
            //So instead I just made it read the raw bytes, stored in a list
            var uStringSize = chunk.ReadUInt32();
            for (var i = 0; i < uStringSize; i++)
                instance.PMarkerName.Add(chunk.ReadByte());

            return instance;
        }
    }

    public class MusicNodeParams
    {
        public byte UFlags { get; set; }
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public AkMeterInfo AkMeterInfo { get; set; }
        public byte BMeterInfoFlag { get; set; }
        public List<CAkStinger> PStingersList { get; set; } = [];

        public static MusicNodeParams Create(ByteChunk chunk)
        {
            var instance = new MusicNodeParams();
            instance.UFlags = chunk.ReadByte();
            instance.NodeBaseParams = NodeBaseParams.Create(chunk);
            instance.Children = Children.Create(chunk);
            instance.AkMeterInfo = AkMeterInfo.Create(chunk);
            instance.BMeterInfoFlag = chunk.ReadByte();

            var numStingers = chunk.ReadUInt32();
            for (var i = 0; i < numStingers; i++)
                instance.PStingersList.Add(CAkStinger.Create(chunk));

            return instance;
        }
    }

    public class AkMeterInfo
    {
        public double FGridPeriod { get; set; }
        public double FGridOffset { get; set; }
        public float FTempo { get; set; }
        public byte UTimeSigNumBeatsBar { get; set; }
        public byte UTimeSigBeatValue { get; set; }

        public static AkMeterInfo Create(ByteChunk chunk)
        {
            var instance = new AkMeterInfo();
            instance.FGridPeriod = chunk.ReadInt64(); //chunk.ReadDouble();
            instance.FGridOffset = chunk.ReadInt64(); //chunk.ReadDouble();
            instance.FTempo = chunk.ReadSingle();
            instance.UTimeSigNumBeatsBar = chunk.ReadByte();
            instance.UTimeSigBeatValue = chunk.ReadByte();

            return instance;
        }
    }

    public class CAkStinger
    {
        public uint TriggerID { get; set; }
        public uint SegmentID { get; set; }
        public uint SyncPlayAt { get; set; }
        public uint UCueFilterHash { get; set; }
        public int DontRepeatTime { get; set; }
        public uint NumSegmentLookAhead { get; set; }

        public static CAkStinger Create(ByteChunk chunk)
        {
            var instance = new CAkStinger();
            instance.TriggerID = chunk.ReadUInt32();
            instance.SegmentID = chunk.ReadUInt32();
            instance.SyncPlayAt = chunk.ReadUInt32();
            instance.UCueFilterHash = chunk.ReadUInt32();
            instance.DontRepeatTime = chunk.ReadInt32();
            instance.NumSegmentLookAhead = chunk.ReadUInt32();

            return instance;
        }
    }
}
