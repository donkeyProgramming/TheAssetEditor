using Shared.ByteParsing;
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

            // A switch track carries its switch association table and transition rule between
            // the track type and the look-ahead time.
            if (TrackType == SwitchTrackType)
            {
                SwitchParams = AkMusicTrackSwitchParams_V136.ReadData(chunk);
                TransRule = AkMusicTrackTransRule_V136.ReadData(chunk);
            }

            LookAheadTime = chunk.ReadInt32();
        }

        public const byte SwitchTrackType = 3;

        public AkMusicTrackSwitchParams_V136? SwitchParams { get; set; }
        public AkMusicTrackTransRule_V136? TransRule { get; set; }

        public override byte[] WriteData()
        {
            var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(Flags, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)SourceList.Count, out _));
            foreach (var source in SourceList)
                memStream.Write(source.WriteData());

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)PlaylistList.Count, out _));
            foreach (var playlistItem in PlaylistList)
                memStream.Write(playlistItem.WriteData());

            // The sub-track count is only on disk when there is a playlist, as in ReadData.
            if (PlaylistList.Count > 0)
                memStream.Write(ByteParsers.UInt32.EncodeValue(NumSubTrack, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)ItemsList.Count, out _));
            foreach (var clipAutomation in ItemsList)
                memStream.Write(clipAutomation.WriteData());

            memStream.Write(NodeBaseParams.WriteData());
            memStream.Write(ByteParsers.Byte.EncodeValue(TrackType, out _));

            if (TrackType == SwitchTrackType)
            {
                memStream.Write(SwitchParams!.WriteData());
                memStream.Write(TransRule!.WriteData());
            }

            memStream.Write(ByteParsers.Int32.EncodeValue(LookAheadTime, out _));
            return memStream.ToArray();
        }

        public override void UpdateSectionSize()
        {
            var size = 1u // Flags
                + 4 + (uint)SourceList.Sum(x => x.GetSize())
                + 4 + (uint)PlaylistList.Count * AkTrackSrcInfo_V136.Size
                + (PlaylistList.Count > 0 ? 4u : 0u)
                + 4 + (uint)ItemsList.Sum(x => x.GetSize())
                + NodeBaseParams.GetSize()
                + 1 // TrackType
                + (TrackType == SwitchTrackType ? SwitchParams!.GetSize() + AkMusicTrackTransRule_V136.Size : 0u)
                + 4; // LookAheadTime

            SectionSize = size + ByteHelper.GetPropertyTypeSize(Id);
        }

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

            // 3 ids + 4 doubles
            public const uint Size = 44;

            public static AkTrackSrcInfo_V136 ReadData(ByteChunk chunk)
            {
                var akTrackSrcInfo = new AkTrackSrcInfo_V136()
                {
                    TrackId = chunk.ReadUInt32(),
                    SourceId = chunk.ReadUInt32(),
                    EventId = chunk.ReadUInt32(),
                    PlayAt = BitConverter.Int64BitsToDouble(chunk.ReadInt64()),
                    BeginTrimOffset = BitConverter.Int64BitsToDouble(chunk.ReadInt64()),
                    EndTrimOffset = BitConverter.Int64BitsToDouble(chunk.ReadInt64()),
                    SrcDuration = BitConverter.Int64BitsToDouble(chunk.ReadInt64()),
                };
                return akTrackSrcInfo;
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(TrackId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(SourceId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(EventId, out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(PlayAt), out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(BeginTrimOffset), out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(EndTrimOffset), out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(SrcDuration), out _));
                return memStream.ToArray();
            }
        }

        /// <summary>Which switch each sub-track is associated with, present only on a switch
        /// track (eTrackType 3).</summary>
        public class AkMusicTrackSwitchParams_V136
        {
            public byte GroupType { get; set; }
            public uint GroupId { get; set; }
            public uint DefaultSwitch { get; set; }
            public List<uint> SwitchAssoc { get; set; } = [];

            public static AkMusicTrackSwitchParams_V136 ReadData(ByteChunk chunk)
            {
                var result = new AkMusicTrackSwitchParams_V136
                {
                    GroupType = chunk.ReadByte(),
                    GroupId = chunk.ReadUInt32(),
                    DefaultSwitch = chunk.ReadUInt32()
                };

                var numSwitchAssoc = chunk.ReadUInt32();
                for (var i = 0; i < numSwitchAssoc; i++)
                    result.SwitchAssoc.Add(chunk.ReadUInt32());

                return result;
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Byte.EncodeValue(GroupType, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(GroupId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(DefaultSwitch, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)SwitchAssoc.Count, out _));
                foreach (var switchId in SwitchAssoc)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(switchId, out _));
                return memStream.ToArray();
            }

            public uint GetSize() => 13 + (uint)SwitchAssoc.Count * 4;
        }

        /// <summary>The fade in/out rule used when a switch track changes sub-track.</summary>
        public class AkMusicTrackTransRule_V136
        {
            public AkMusicFade_V136 SrcFade { get; set; } = new AkMusicFade_V136();
            public uint SyncType { get; set; }
            public uint CueFilterHash { get; set; }
            public AkMusicFade_V136 DestFade { get; set; } = new AkMusicFade_V136();

            // two fades (12 each) + SyncType + CueFilterHash
            public const uint Size = 32;

            public static AkMusicTrackTransRule_V136 ReadData(ByteChunk chunk)
            {
                return new AkMusicTrackTransRule_V136
                {
                    SrcFade = AkMusicFade_V136.ReadData(chunk),
                    SyncType = chunk.ReadUInt32(),
                    CueFilterHash = chunk.ReadUInt32(),
                    DestFade = AkMusicFade_V136.ReadData(chunk)
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(SrcFade.WriteData());
                memStream.Write(ByteParsers.UInt32.EncodeValue(SyncType, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(CueFilterHash, out _));
                memStream.Write(DestFade.WriteData());
                return memStream.ToArray();
            }
        }

        public class AkMusicFade_V136
        {
            public int TransitionTime { get; set; }
            public uint FadeCurve { get; set; }
            public int FadeOffset { get; set; }

            public static AkMusicFade_V136 ReadData(ByteChunk chunk)
            {
                return new AkMusicFade_V136
                {
                    TransitionTime = chunk.ReadInt32(),
                    FadeCurve = chunk.ReadUInt32(),
                    FadeOffset = chunk.ReadInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Int32.EncodeValue(TransitionTime, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(FadeCurve, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(FadeOffset, out _));
                return memStream.ToArray();
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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(ClipIndex, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(AutoType, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)RtpcMgr.Count, out _));
                foreach (var graphPoint in RtpcMgr)
                    memStream.Write(graphPoint.WriteData());
                return memStream.ToArray();
            }

            // ClipIndex + AutoType + point count, then the points.
            public uint GetSize() => 12 + (uint)RtpcMgr.Count * AkRtpcGraphPoint_V136.Size;
        }
    }
}
