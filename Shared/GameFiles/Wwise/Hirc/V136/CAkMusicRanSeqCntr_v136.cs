using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkMusicRanSeqCntr_v136 : HircItem
    {
        public MusicTransNodeParams MusicTransNodeParams { get; set; }
        public List<AkMusicRanSeqPlaylistItem> PPlayList { get; set; } = [];

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            MusicTransNodeParams = MusicTransNodeParams.Create(chunk);

            //this is like a tree, numPlaylistItems is the total in the tree... I think
            var numPlaylistItems = chunk.ReadUInt32();
            //and the root always has 1, again I think...
            //for (int i = 0; i < numPlaylistItems; i++)
            PPlayList.Add(AkMusicRanSeqPlaylistItem.Create(chunk));
        }

        public override void UpdateSectionSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class MusicTransNodeParams
    {
        public MusicNodeParams MusicNodeParams { get; set; }
        public List<AkMusicTransitionRule> PPlayList { get; set; } = [];

        public static MusicTransNodeParams Create(ByteChunk chunk)
        {
            var instance = new MusicTransNodeParams();

            instance.MusicNodeParams = MusicNodeParams.Create(chunk);

            var numRules = chunk.ReadUInt32();
            for (var i = 0; i < numRules; i++)
                instance.PPlayList.Add(AkMusicTransitionRule.Create(chunk));

            return instance;
        }
    }

    public class AkMusicTransitionRule
    {
        public uint UNumSrc { get; set; }
        public List<uint> SrcIdList { get; set; } = [];
        public uint UNumDst { get; set; }
        public List<uint> DstIdList { get; set; } = [];
        public AkMusicTransSrcRule AkMusicTransSrcRule { get; set; }
        public AkMusicTransDstRule AkMusicTransDstRule { get; set; }
        public uint UlStateGroupIdCustom { get; set; }
        public uint UlStateIdCustom { get; set; }
        public byte AllocTransObjectFlag { get; set; }
        public AkMusicTransitionObject AkMusicTransitionObject { get; set; }

        public static AkMusicTransitionRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransitionRule();

            var uNumSrc = chunk.ReadUInt32();
            for (var i = 0; i < uNumSrc; i++)
                instance.SrcIdList.Add(chunk.ReadUInt32());

            var uNumDst = chunk.ReadUInt32();
            for (var i = 0; i < uNumDst; i++)
                instance.DstIdList.Add(chunk.ReadUInt32());

            instance.AkMusicTransSrcRule = AkMusicTransSrcRule.Create(chunk);

            instance.AkMusicTransDstRule = AkMusicTransDstRule.Create(chunk);

            instance.UlStateGroupIdCustom = chunk.ReadUInt32();
            instance.UlStateIdCustom = chunk.ReadUInt32();

            var allocTransObjectFlag = chunk.ReadByte();
            var has_transobj = allocTransObjectFlag != 0;
            if (has_transobj)
                instance.AkMusicTransitionObject = AkMusicTransitionObject.Create(chunk);

            return instance;
        }
    }

    public class AkMusicTransitionObject
    {
        public int SegmentId { get; set; }
        public AkMusicFade FadeInParams { get; set; }
        public AkMusicFade FadeOutParams { get; set; }
        public byte BPlayPreEntry { get; set; }
        public byte BPlayPostExit { get; set; }

        public static AkMusicTransitionObject Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransitionObject();

            instance.SegmentId = chunk.ReadInt32();
            instance.FadeInParams = AkMusicFade.Create(chunk);
            instance.FadeOutParams = AkMusicFade.Create(chunk);

            instance.BPlayPreEntry = chunk.ReadByte();
            instance.BPlayPostExit = chunk.ReadByte();

            return instance;
        }
    }

    public class AkMusicFade
    {
        public int TransitionTime { get; set; }
        public uint EFadeCurve { get; set; }
        public int IFadeOffset { get; set; }

        public static AkMusicFade Create(ByteChunk chunk)
        {
            var instance = new AkMusicFade();
            instance.TransitionTime = chunk.ReadInt32();
            instance.EFadeCurve = chunk.ReadUInt32();
            instance.IFadeOffset = chunk.ReadInt32();
            return instance;
        }
    }

    public class AkMusicTransSrcRule
    {
        public int TransitionTime { get; set; }
        public uint EFadeCurve { get; set; }
        public int IFadeOffset { get; set; }
        public uint ESyncType { get; set; }
        public uint UCueFilterHash { get; set; }
        public byte BPlayPostExit { get; set; }

        public static AkMusicTransSrcRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransSrcRule();
            instance.TransitionTime = chunk.ReadInt32();
            instance.EFadeCurve = chunk.ReadUInt32();
            instance.IFadeOffset = chunk.ReadInt32();
            instance.ESyncType = chunk.ReadUInt32();
            instance.UCueFilterHash = chunk.ReadUInt32();
            instance.BPlayPostExit = chunk.ReadByte();
            return instance;
        }
    }

    public class AkMusicTransDstRule
    {

        public int TransitionTime { get; set; }
        public uint EFadeCurve { get; set; }
        public int IFadeOffset { get; set; }
        public uint UCueFilterHash { get; set; }
        public uint UJumpToId { get; set; }
        public ushort EJumpToType { get; set; }
        public ushort EEntryType { get; set; }
        public byte BPlayPreEntry { get; set; }
        public byte BDestMatchSourceCueName { get; set; }

        public static AkMusicTransDstRule Create(ByteChunk chunk)
        {
            var instance = new AkMusicTransDstRule();
            instance.TransitionTime = chunk.ReadInt32();
            instance.EFadeCurve = chunk.ReadUInt32();
            instance.IFadeOffset = chunk.ReadInt32();
            instance.UCueFilterHash = chunk.ReadUInt32();
            instance.UJumpToId = chunk.ReadUInt32();
            instance.EJumpToType = chunk.ReadUShort();
            instance.EEntryType = chunk.ReadUShort();
            instance.BPlayPreEntry = chunk.ReadByte();
            instance.BDestMatchSourceCueName = chunk.ReadByte();
            return instance;
        }
    }

    public class AkMusicRanSeqPlaylistItem
    {
        public uint SegmentId { get; set; }
        public int PlaylistItemId { get; set; }
        public uint ERsType { get; set; }
        public short Loop { get; set; }
        public short LoopMin { get; set; }
        public short LoopMax { get; set; }
        public uint Weight { get; set; }
        public ushort WAvoidRepeatCount { get; set; }
        public byte BIsUsingWeight { get; set; }
        public byte BIsShuffle { get; set; }
        public List<AkMusicRanSeqPlaylistItem> PPlayList { get; set; } = [];

        public static AkMusicRanSeqPlaylistItem Create(ByteChunk chunk)
        {
            var instance = new AkMusicRanSeqPlaylistItem();

            instance.SegmentId = chunk.ReadUInt32();
            instance.PlaylistItemId = chunk.ReadInt32();
            var numChildren = chunk.ReadUInt32();
            instance.ERsType = chunk.ReadUInt32();
            instance.Loop = chunk.ReadShort();
            instance.LoopMin = chunk.ReadShort();
            instance.LoopMax = chunk.ReadShort();
            instance.Weight = chunk.ReadUInt32();
            instance.WAvoidRepeatCount = chunk.ReadUShort();
            instance.BIsUsingWeight = chunk.ReadByte();
            instance.BIsShuffle = chunk.ReadByte();

            for (var i = 0; i < numChildren; i++)
                instance.PPlayList.Add(Create(chunk));

            return instance;
        }
    }
}
