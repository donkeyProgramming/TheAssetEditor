using Shared.ByteParsing;

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

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(MusicNodeParams.WriteData());
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)PlayList.Count, out _));
            foreach (var rule in PlayList)
                memStream.Write(rule.WriteData());
            return memStream.ToArray();
        }

        public uint GetSize() =>
            MusicNodeParams.GetSize()
            + 4 // NumRules
            + (uint)PlayList.Sum(rule => rule.GetSize());

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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)SrcIdList.Count, out _));
                foreach (var srcId in SrcIdList)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(srcId, out _));

                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)DstIdList.Count, out _));
                foreach (var dstId in DstIdList)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(dstId, out _));

                memStream.Write(AkMusicTransSrcRule.WriteData());
                memStream.Write(AkMusicTransDstRule.WriteData());
                memStream.Write(ByteParsers.UInt32.EncodeValue(StateGroupIdCustom, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(StateIdCustom, out _));

                // Derived from the object rather than the parsed flag, so adding or removing a
                // transition object keeps the two in step.
                var hasTransitionObject = AkMusicTransitionObject != null;
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)(hasTransitionObject ? 1 : 0), out _));
                if (hasTransitionObject)
                    memStream.Write(AkMusicTransitionObject!.WriteData());

                return memStream.ToArray();
            }

            public uint GetSize() =>
                4 + (uint)SrcIdList.Count * 4
                + 4 + (uint)DstIdList.Count * 4
                + AkMusicTransSrcRule_V136.Size
                + AkMusicTransDstRule_V136.Size
                + 4 // StateGroupIdCustom
                + 4 // StateIdCustom
                + 1 // AllocTransObjectFlag
                + (AkMusicTransitionObject != null ? AkMusicTransitionObject_V136.Size : 0u);

            public class AkMusicTransSrcRule_V136
            {
                public int TransitionTime { get; set; }
                public uint FadeCurve { get; set; }
                public int FadeOffset { get; set; }
                public uint SyncType { get; set; }
                public uint CueFilterHash { get; set; }
                public byte PlayPostExit { get; set; }

                // 5 x 4 + 1
                public const uint Size = 21;

                public void ReadData(ByteChunk chunk)
                {
                    TransitionTime = chunk.ReadInt32();
                    FadeCurve = chunk.ReadUInt32();
                    FadeOffset = chunk.ReadInt32();
                    SyncType = chunk.ReadUInt32();
                    CueFilterHash = chunk.ReadUInt32();
                    PlayPostExit = chunk.ReadByte();
                }

                public byte[] WriteData()
                {
                    using var memStream = new MemoryStream();
                    memStream.Write(ByteParsers.Int32.EncodeValue(TransitionTime, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(FadeCurve, out _));
                    memStream.Write(ByteParsers.Int32.EncodeValue(FadeOffset, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(SyncType, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(CueFilterHash, out _));
                    memStream.Write(ByteParsers.Byte.EncodeValue(PlayPostExit, out _));
                    return memStream.ToArray();
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

                // 5 x 4 + 2 x 2 + 2 x 1
                public const uint Size = 26;

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

                public byte[] WriteData()
                {
                    using var memStream = new MemoryStream();
                    memStream.Write(ByteParsers.Int32.EncodeValue(TransitionTime, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(FadeCurve, out _));
                    memStream.Write(ByteParsers.Int32.EncodeValue(FadeOffset, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(CueFilterHash, out _));
                    memStream.Write(ByteParsers.UInt32.EncodeValue(JumpToId, out _));
                    memStream.Write(ByteParsers.UShort.EncodeValue(JumpToType, out _));
                    memStream.Write(ByteParsers.UShort.EncodeValue(EntryType, out _));
                    memStream.Write(ByteParsers.Byte.EncodeValue(PlayPreEntry, out _));
                    memStream.Write(ByteParsers.Byte.EncodeValue(DestMatchSourceCueName, out _));
                    return memStream.ToArray();
                }
            }

            public class AkMusicTransitionObject_V136
            {
                public int SegmentId { get; set; }
                public AkMusicFade_V136 FadeInParams { get; set; } = new AkMusicFade_V136();
                public AkMusicFade_V136 FadeOutParams { get; set; } = new AkMusicFade_V136();
                public byte PlayPreEntry { get; set; }
                public byte PlayPostExit { get; set; }

                // SegmentId + two fades (12 each) + 2 x 1
                public const uint Size = 30;

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

                public byte[] WriteData()
                {
                    using var memStream = new MemoryStream();
                    memStream.Write(ByteParsers.Int32.EncodeValue(SegmentId, out _));
                    memStream.Write(FadeInParams.WriteData());
                    memStream.Write(FadeOutParams.WriteData());
                    memStream.Write(ByteParsers.Byte.EncodeValue(PlayPreEntry, out _));
                    memStream.Write(ByteParsers.Byte.EncodeValue(PlayPostExit, out _));
                    return memStream.ToArray();
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
            }
        }
    }
}
