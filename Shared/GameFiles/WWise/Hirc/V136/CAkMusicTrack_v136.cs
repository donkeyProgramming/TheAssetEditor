using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkMusicTrack_v136 : HircItem, INodeBaseParamsAccessor, ICAkMusicTrack
    {
        public byte UFlags { get; set; }

        public List<AkBankSourceData> PSourceList { get; set; } = new List<AkBankSourceData>();

        public List<AkTrackSrcInfo> PPlaylistList { get; set; } = new List<AkTrackSrcInfo>();

        public uint NumSubTrack { get; set; }

        public List<AkClipAutomation> PItemsList { get; set; } = new List<AkClipAutomation>();

        public NodeBaseParams NodeBaseParams { get; set; }

        public byte ETrackType { get; set; }
        public int ILookAheadTime { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            UFlags = chunk.ReadByte();

            var numSources = chunk.ReadUInt32();
            for (var i = 0; i < numSources; i++)
                PSourceList.Add(AkBankSourceData.Create(chunk));

            var numPlaylistItem = chunk.ReadUInt32();
            for (var i = 0; i < numPlaylistItem; i++)
                PPlaylistList.Add(AkTrackSrcInfo.Create(chunk));

            if (numPlaylistItem > 0)
                NumSubTrack = chunk.ReadUInt32();

            var numClipAutomationItem = chunk.ReadUInt32();
            for (var i = 0; i < numClipAutomationItem; i++)
                PItemsList.Add(AkClipAutomation.Create(chunk));

            NodeBaseParams = NodeBaseParams.Create(chunk);

            ETrackType = chunk.ReadByte();

            ILookAheadTime = chunk.ReadInt32();
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();

        public List<uint> GetChildren() => PSourceList.Select(x => x.akMediaInformation.SourceId).ToList();
    }

    public class AkTrackSrcInfo
    {
        public uint TrackID { get; set; }
        public uint SourceID { get; set; }
        public uint EventID { get; set; }
        public double FPlayAt { get; set; }
        public double FBeginTrimOffset { get; set; }
        public double FEndTrimOffset { get; set; }
        public double FSrcDuration { get; set; }

        public static AkTrackSrcInfo Create(ByteChunk chunk)
        {
            var output = new AkTrackSrcInfo()
            {
                TrackID = chunk.ReadUInt32(),
                SourceID = chunk.ReadUInt32(),
                EventID = chunk.ReadUInt32(),
                FPlayAt = chunk.ReadInt64(), //chunk.ReadDouble()
                FBeginTrimOffset = chunk.ReadInt64(), //chunk.ReadDouble()
                FEndTrimOffset = chunk.ReadInt64(), //chunk.ReadDouble()
                FSrcDuration = chunk.ReadInt64(), //chunk.ReadDouble()
            };
            return output;
        }
    }

    public class AkClipAutomation
    {
        public uint UClipIndex { get; set; }
        public uint EAutoType { get; set; }
        public List<AkRTPCGraphPoint> PRTPCMgr { get; set; } = [];

        public static AkClipAutomation Create(ByteChunk chunk)
        {
            var instance = new AkClipAutomation();
            instance.UClipIndex = chunk.ReadUInt32();
            instance.EAutoType = chunk.ReadUInt32();
            var uNumPoints = chunk.ReadUInt32();
            for (var i = 0; i < uNumPoints; i++)
                instance.PRTPCMgr.Add(AkRTPCGraphPoint.Create(chunk));

            return instance;
        }
    }
}
