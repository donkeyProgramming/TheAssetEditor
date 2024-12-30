using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkMusicTrack_v136 : HircItem, ICAkMusicTrack
    {
        public byte UFlags { get; set; }
        public List<AkBankSourceData> PSourceList { get; set; } = [];
        public List<AkTrackSrcInfo> PPlaylistList { get; set; } = [];
        public uint NumSubTrack { get; set; }
        public List<AkClipAutomation> PItemsList { get; set; } = [];
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

        public List<uint> GetChildren() => PSourceList.Select(x => x.AkMediaInformation.SourceId).ToList();
    }
    public class AkTrackSrcInfo
    {
        public uint TrackId { get; set; }
        public uint SourceId { get; set; }
        public uint EventId { get; set; }
        public double FPlayAt { get; set; }
        public double FBeginTrimOffset { get; set; }
        public double FEndTrimOffset { get; set; }
        public double FSrcDuration { get; set; }

        public static AkTrackSrcInfo Create(ByteChunk chunk)
        {
            var output = new AkTrackSrcInfo()
            {
                TrackId = chunk.ReadUInt32(),
                SourceId = chunk.ReadUInt32(),
                EventId = chunk.ReadUInt32(),
                FPlayAt = chunk.ReadInt64(),
                FBeginTrimOffset = chunk.ReadInt64(),
                FEndTrimOffset = chunk.ReadInt64(),
                FSrcDuration = chunk.ReadInt64(),
            };
            return output;
        }
    }

    public class AkClipAutomation
    {
        public uint UClipIndex { get; set; }
        public uint EAutoType { get; set; }
        public List<AkRtpcGraphPoint> PRtpcMgr { get; set; } = [];

        public static AkClipAutomation Create(ByteChunk chunk)
        {
            var instance = new AkClipAutomation();
            instance.UClipIndex = chunk.ReadUInt32();
            instance.EAutoType = chunk.ReadUInt32();
            var uNumPoints = chunk.ReadUInt32();
            for (var i = 0; i < uNumPoints; i++)
                instance.PRtpcMgr.Add(AkRtpcGraphPoint.Create(chunk));
            return instance;
        }
    }
}
