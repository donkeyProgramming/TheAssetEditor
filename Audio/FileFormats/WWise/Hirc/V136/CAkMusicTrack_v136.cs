using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkMusicTrack_v136 : HircItem
    {

        public byte uFlags { get; set; }

        public List<AkBankSourceData> pSourceList { get; set; } = new List<AkBankSourceData>();

        public List<AkTrackSrcInfo> pPlaylistList { get; set; } = new List<AkTrackSrcInfo>();

        public uint numSubTrack { get; set; }

        public List<AkClipAutomation> pItemsList { get; set; } = new List<AkClipAutomation>();

        public NodeBaseParams NodeBaseParams { get; set; }

        public byte eTrackType { get; set; }
        public int iLookAheadTime { get; set; }


        protected override void CreateSpesificData(ByteChunk chunk)
        {
            uFlags = chunk.ReadByte();

            var numSources = chunk.ReadUInt32();
            for (int i = 0; i < numSources; i++)
                pSourceList.Add(AkBankSourceData.Create(chunk));

            var numPlaylistItem = chunk.ReadUInt32();
            for (int i = 0; i < numPlaylistItem; i++)
                pPlaylistList.Add(AkTrackSrcInfo.Create(chunk));

            if (numPlaylistItem > 0)
                numSubTrack = chunk.ReadUInt32();

            var numClipAutomationItem = chunk.ReadUInt32();
            for (int i = 0; i < numClipAutomationItem; i++)
                pItemsList.Add(AkClipAutomation.Create(chunk));

            NodeBaseParams = NodeBaseParams.Create(chunk);

            eTrackType = chunk.ReadByte();

            iLookAheadTime = chunk.ReadInt32();
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkTrackSrcInfo
    {
        public uint trackID { get; set; }
        public uint sourceID { get; set; }
        public uint eventID { get; set; }
        public double fPlayAt { get; set; }
        public double fBeginTrimOffset { get; set; }
        public double fEndTrimOffset { get; set; }
        public double fSrcDuration { get; set; }

        public static AkTrackSrcInfo Create(ByteChunk chunk)
        {
            var output = new AkTrackSrcInfo()
            {
                trackID = chunk.ReadUInt32(),
                sourceID = chunk.ReadUInt32(),
                eventID = chunk.ReadUInt32(),
                fPlayAt = chunk.ReadInt64(), //chunk.ReadDouble()
                fBeginTrimOffset = chunk.ReadInt64(), //chunk.ReadDouble()
                fEndTrimOffset = chunk.ReadInt64(), //chunk.ReadDouble()
                fSrcDuration = chunk.ReadInt64(), //chunk.ReadDouble()
            };
            return output;
        }
    }

    public class AkClipAutomation
    {
        public uint uClipIndex { get; set; }
        public uint eAutoType { get; set; }
        public List<AkRTPCGraphPoint> pRTPCMgr { get; set; } = new List<AkRTPCGraphPoint>();

        public static AkClipAutomation Create(ByteChunk chunk)
        {
            var instance = new AkClipAutomation();
            instance.uClipIndex = chunk.ReadUInt32();
            instance.eAutoType = chunk.ReadUInt32();
            var uNumPoints = chunk.ReadUInt32();
            for (int i = 0; i < uNumPoints; i++)
                instance.pRTPCMgr.Add(AkRTPCGraphPoint.Create(chunk));

            return instance;
        }
    }
}