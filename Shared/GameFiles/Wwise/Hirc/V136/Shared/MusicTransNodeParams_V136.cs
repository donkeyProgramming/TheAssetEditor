using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class MusicTransNodeParams_V136
    {
        public MusicNodeParams_V136 MusicNodeParams { get; set; } = new MusicNodeParams_V136();
        public uint NumRules { get; set; }
        public List<AkMusicTransitionRule_V136> PlayList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            MusicNodeParams.ReadData(chunk);
            NumRules = chunk.ReadUInt32();
            for (var i = 0; i < NumRules; i++)
            {
                var akMusicTransitionRule = new AkMusicTransitionRule_V136();
                akMusicTransitionRule.ReadData(chunk);
                PlayList.Add(akMusicTransitionRule);
            }
        }

        public class AkMusicTransitionRule_V136
        {
            public uint NumSrc { get; set; }
            public List<uint> SrcIdList { get; set; } = [];
            public uint NumDst { get; set; }
            public List<uint> DstIdList { get; set; } = [];
            public AkMusicTransSrcRule_V136 AkMusicTransSrcRule { get; set; } = new AkMusicTransSrcRule_V136();
            public AkMusicTransDstRule_V136 AkMusicTransDstRule { get; set; } = new AkMusicTransDstRule_V136();
            public uint StateGroupIdCustom { get; set; }
            public uint StateIdCustom { get; set; }
            public byte AllocTransObjectFlag { get; set; }
            public AkMusicTransitionObject_V136? AkMusicTransitionObject { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                NumSrc = chunk.ReadUInt32();
                for (var i = 0; i < NumSrc; i++)
                    SrcIdList.Add(chunk.ReadUInt32());

                NumDst = chunk.ReadUInt32();
                for (var i = 0; i < NumDst; i++)
                    DstIdList.Add(chunk.ReadUInt32());

                AkMusicTransSrcRule.ReadData(chunk);
                AkMusicTransDstRule.ReadData(chunk);
                StateGroupIdCustom = chunk.ReadUInt32();
                StateIdCustom = chunk.ReadUInt32();

                var allocTransObjectFlag = chunk.ReadByte();
                var has_transobj = allocTransObjectFlag != 0;
                if (has_transobj)
                    AkMusicTransitionObject = AkMusicTransitionObject_V136.ReadData(chunk);
            }

            public class AkMusicTransSrcRule_V136
            {
                public int TransitionTime { get; set; }
                public uint FadeCurve { get; set; }
                public int FadeOffset { get; set; }
                public uint SyncType { get; set; }
                public uint CueFilterHash { get; set; }
                public byte PlayPostExit { get; set; }

                public void ReadData(ByteChunk chunk)
                {
                    TransitionTime = chunk.ReadInt32();
                    FadeCurve = chunk.ReadUInt32();
                    FadeOffset = chunk.ReadInt32();
                    SyncType = chunk.ReadUInt32();
                    CueFilterHash = chunk.ReadUInt32();
                    PlayPostExit = chunk.ReadByte();
                }
            }

            public class AkMusicTransDstRule_V136
            {
                public int TransitionTime { get; set; }
                public uint FadeCurve { get; set; }
                public int FadeOffset { get; set; }
                public uint CueFilterHash { get; set; }
                public uint JumpToId { get; set; }
                public ushort JumpToType { get; set; }
                public ushort EntryType { get; set; }
                public byte PlayPreEntry { get; set; }
                public byte DestMatchSourceCueName { get; set; }

                public void ReadData(ByteChunk chunk)
                {
                    TransitionTime = chunk.ReadInt32();
                    FadeCurve = chunk.ReadUInt32();
                    FadeOffset = chunk.ReadInt32();
                    CueFilterHash = chunk.ReadUInt32();
                    JumpToId = chunk.ReadUInt32();
                    JumpToType = chunk.ReadUShort();
                    EntryType = chunk.ReadUShort();
                    PlayPreEntry = chunk.ReadByte();
                    DestMatchSourceCueName = chunk.ReadByte();
                }
            }

            public class AkMusicTransitionObject_V136
            {
                public int SegmentId { get; set; }
                public AkMusicFade_V136 FadeInParams { get; set; }
                public AkMusicFade_V136 FadeOutParams { get; set; }
                public byte PlayPreEntry { get; set; }
                public byte PlayPostExit { get; set; }

                public static AkMusicTransitionObject_V136 ReadData(ByteChunk chunk)
                {
                    return new AkMusicTransitionObject_V136
                    {
                        SegmentId = chunk.ReadInt32(),
                        FadeInParams = AkMusicFade_V136.ReadData(chunk),
                        FadeOutParams = AkMusicFade_V136.ReadData(chunk),
                        PlayPreEntry = chunk.ReadByte(),
                        PlayPostExit = chunk.ReadByte()
                    };
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
                }
            }
        }
    }
}
