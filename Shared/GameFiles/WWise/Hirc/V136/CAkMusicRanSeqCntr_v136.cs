using Shared.Core.ByteParsing;
using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkMusicRanSeqCntr_v136 : HircItem
    {
        public MusicTransNodeParams MusicTransNodeParams { get; set; }
        public List<AkMusicRanSeqPlaylistItem> pPlayList { get; set; } = new List<AkMusicRanSeqPlaylistItem>();


        protected override void CreateSpecificData(ByteChunk chunk)
        {
            MusicTransNodeParams = MusicTransNodeParams.Create(chunk);

            //this is like a tree, numPlaylistItems is the total in the tree... I think
            var numPlaylistItems = chunk.ReadUInt32();
            //and the root always has 1, again I think...
            //for (int i = 0; i < numPlaylistItems; i++)
            pPlayList.Add(AkMusicRanSeqPlaylistItem.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
    public class MusicTransNodeParams
    {
        public MusicNodeParams MusicNodeParams { get; set; }
        public List<AkMusicTransitionRule> pPlayList { get; set; } = new List<AkMusicTransitionRule>();

        public static MusicTransNodeParams Create(ByteChunk chunk)
        {
            var instance = new MusicTransNodeParams();

            instance.MusicNodeParams = MusicNodeParams.Create(chunk);

            var numRules = chunk.ReadUInt32();
            for (int i = 0; i < numRules; i++)
                instance.pPlayList.Add(AkMusicTransitionRule.Create(chunk));

            return instance;
        }
    }

    public class AkMusicTransitionRule
    {
        public uint uNumSrc { get; set; }
        public List<uint> srcIDList { get; set; } = new List<uint>();

        public uint uNumDst { get; set; }
        public List<uint> dstIDList { get; set; } = new List<uint>();
        public AkMusicTransSrcRule AkMusicTransSrcRule { get; set; }
        public AkMusicTransDstRule AkMusicTransDstRule { get; set; }

        public uint ulStateGroupID_custom { get; set; }
        public uint ulStateID_custom { get; set; }
        public byte AllocTransObjectFlag { get; set; }
        public AkMusicTransitionObject AkMusicTransitionObject { get; set; }

        public static AkMusicTransitionRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransitionRule();

            var uNumSrc = chunk.ReadUInt32();
            for (int i = 0; i < uNumSrc; i++)
                instance.srcIDList.Add(chunk.ReadUInt32());

            var uNumDst = chunk.ReadUInt32();
            for (int i = 0; i < uNumDst; i++)
                instance.dstIDList.Add(chunk.ReadUInt32());

            instance.AkMusicTransSrcRule = AkMusicTransSrcRule.Create(chunk);

            instance.AkMusicTransDstRule = AkMusicTransDstRule.Create(chunk);


            instance.ulStateGroupID_custom = chunk.ReadUInt32();
            instance.ulStateID_custom = chunk.ReadUInt32();

            var AllocTransObjectFlag = chunk.ReadByte();
            var has_transobj = AllocTransObjectFlag != 0;
            if (has_transobj)
                instance.AkMusicTransitionObject = AkMusicTransitionObject.Create(chunk);

            return instance;
        }
    }

    public class AkMusicTransitionObject
    {
        public int segmentID { get; set; }
        public AkMusicFade fadeInParams { get; set; }
        public AkMusicFade fadeOutParams { get; set; }
        public byte bPlayPreEntry { get; set; }
        public byte bPlayPostExit { get; set; }

        public static AkMusicTransitionObject Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransitionObject();

            instance.segmentID = chunk.ReadInt32();
            instance.fadeInParams = AkMusicFade.Create(chunk);
            instance.fadeOutParams = AkMusicFade.Create(chunk);

            instance.bPlayPreEntry = chunk.ReadByte();
            instance.bPlayPostExit = chunk.ReadByte();

            return instance;
        }
    }

    public class AkMusicFade
    {
        public int transitionTime { get; set; }
        public uint eFadeCurve { get; set; }
        public int iFadeOffset { get; set; }


        public static AkMusicFade Create(ByteChunk chunk)
        {
            var instance = new AkMusicFade();

            instance.transitionTime = chunk.ReadInt32();
            instance.eFadeCurve = chunk.ReadUInt32();
            instance.iFadeOffset = chunk.ReadInt32();

            return instance;
        }
    }

    public class AkMusicTransSrcRule
    {
        public int transitionTime { get; set; }
        public uint eFadeCurve { get; set; }
        public int iFadeOffset { get; set; }
        public uint eSyncType { get; set; }
        public uint uCueFilterHash { get; set; }
        public byte bPlayPostExit { get; set; }


        public static AkMusicTransSrcRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransSrcRule();

            instance.transitionTime = chunk.ReadInt32();
            instance.eFadeCurve = chunk.ReadUInt32();
            instance.iFadeOffset = chunk.ReadInt32();
            instance.eSyncType = chunk.ReadUInt32();
            instance.uCueFilterHash = chunk.ReadUInt32();
            instance.bPlayPostExit = chunk.ReadByte();

            return instance;
        }
    }

    public class AkMusicTransDstRule
    {

        public int transitionTime { get; set; }
        public uint eFadeCurve { get; set; }
        public int iFadeOffset { get; set; }
        public uint uCueFilterHash { get; set; }
        public uint uJumpToID { get; set; }
        public ushort eJumpToType { get; set; }
        public ushort eEntryType { get; set; }
        public byte bPlayPreEntry { get; set; }
        public byte bDestMatchSourceCueName { get; set; }


        public static AkMusicTransDstRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransDstRule();

            instance.transitionTime = chunk.ReadInt32();
            instance.eFadeCurve = chunk.ReadUInt32();
            instance.iFadeOffset = chunk.ReadInt32();
            instance.uCueFilterHash = chunk.ReadUInt32();
            instance.uJumpToID = chunk.ReadUInt32();
            instance.eJumpToType = chunk.ReadUShort();
            instance.eEntryType = chunk.ReadUShort();
            instance.bPlayPreEntry = chunk.ReadByte();
            instance.bDestMatchSourceCueName = chunk.ReadByte();

            return instance;
        }
    }

    public class AkMusicRanSeqPlaylistItem
    {
        public uint SegmentID { get; set; }
        public int playlistItemID { get; set; }
        public uint eRSType { get; set; }
        public short Loop { get; set; }
        public short LoopMin { get; set; }
        public short LoopMax { get; set; }
        public uint Weight { get; set; }
        public ushort wAvoidRepeatCount { get; set; }
        public byte bIsUsingWeight { get; set; }
        public byte bIsShuffle { get; set; }

        public List<AkMusicRanSeqPlaylistItem> pPlayList { get; set; } = new List<AkMusicRanSeqPlaylistItem>();


        public static AkMusicRanSeqPlaylistItem Create(ByteChunk chunk)
        {
            var instance = new AkMusicRanSeqPlaylistItem();

            instance.SegmentID = chunk.ReadUInt32();
            instance.playlistItemID = chunk.ReadInt32();
            var NumChildren = chunk.ReadUInt32();
            instance.eRSType = chunk.ReadUInt32();
            instance.Loop = chunk.ReadShort();
            instance.LoopMin = chunk.ReadShort();
            instance.LoopMax = chunk.ReadShort();
            instance.Weight = chunk.ReadUInt32();
            instance.wAvoidRepeatCount = chunk.ReadUShort();
            instance.bIsUsingWeight = chunk.ReadByte();
            instance.bIsShuffle = chunk.ReadByte();

            for (int i = 0; i < NumChildren; i++)
                instance.pPlayList.Add(Create(chunk));

            return instance;
        }
    }
}