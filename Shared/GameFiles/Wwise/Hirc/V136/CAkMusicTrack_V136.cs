using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkMusicTrack_V136 : HircItem, ICAkMusicTrack
    {
        public byte Flags { get; set; }
        public uint NumSources { get; set; }
        public List<AkBankSourceData_V136> SourceList { get; set; } = [];
        public uint NumPlaylistItem { get; set; }
        public List<AkTrackSrcInfo_V136> PlaylistList { get; set; } = [];
        public uint NumSubTrack { get; set; }
        public List<AkClipAutomation_V136> ItemsList { get; set; } = [];
        public NodeBaseParams_V136 NodeBaseParams { get; set; } = new NodeBaseParams_V136();
        public byte TrackType { get; set; }
        public int LookAheadTime { get; set; }

        protected override void ReadData(ByteChunk chunk)
        {
            Flags = chunk.ReadByte();
            NumSources = chunk.ReadUInt32();
            for (var i = 0; i < NumSources; i++)
                SourceList.Add(AkBankSourceData_V136.ReadData(chunk));

            NumPlaylistItem = chunk.ReadUInt32();
            for (var i = 0; i < NumPlaylistItem; i++)
                PlaylistList.Add(AkTrackSrcInfo_V136.ReadData(chunk));

            if (NumPlaylistItem > 0)
                NumSubTrack = chunk.ReadUInt32();

            var numClipAutomationItem = chunk.ReadUInt32();
            for (var i = 0; i < numClipAutomationItem; i++)
                ItemsList.Add(AkClipAutomation_V136.ReadData(chunk));

            NodeBaseParams.ReadData(chunk);
            TrackType = chunk.ReadByte();
            LookAheadTime = chunk.ReadInt32();
        }

        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");

        public List<uint> GetChildren() => SourceList.Select(x => x.AkMediaInformation.SourceId).ToList();

        public class AkTrackSrcInfo_V136
        {
            public uint TrackId { get; set; }
            public uint SourceId { get; set; }
            public uint EventId { get; set; }
            public double PlayAt { get; set; }
            public double BeginTrimOffset { get; set; }
            public double EndTrimOffset { get; set; }
            public double SrcDuration { get; set; }

            public static AkTrackSrcInfo_V136 ReadData(ByteChunk chunk)
            {
                var akTrackSrcInfo = new AkTrackSrcInfo_V136()
                {
                    TrackId = chunk.ReadUInt32(),
                    SourceId = chunk.ReadUInt32(),
                    EventId = chunk.ReadUInt32(),
                    PlayAt = chunk.ReadInt64(),
                    BeginTrimOffset = chunk.ReadInt64(),
                    EndTrimOffset = chunk.ReadInt64(),
                    SrcDuration = chunk.ReadInt64(),
                };
                return akTrackSrcInfo;
            }
        }

        public class AkClipAutomation_V136
        {
            public uint ClipIndex { get; set; }
            public uint AutoType { get; set; }
            public List<AkRtpcGraphPoint_V136> RtpcMgr { get; set; } = [];

            public static AkClipAutomation_V136 ReadData(ByteChunk chunk)
            {
                var akClipAutomation = new AkClipAutomation_V136();
                akClipAutomation.ClipIndex = chunk.ReadUInt32();
                akClipAutomation.AutoType = chunk.ReadUInt32();
                var uNumPoints = chunk.ReadUInt32();
                for (var i = 0; i < uNumPoints; i++)
                    akClipAutomation.RtpcMgr.Add(AkRtpcGraphPoint_V136.ReadData(chunk));
                return akClipAutomation;
            }
        }
    }
}
