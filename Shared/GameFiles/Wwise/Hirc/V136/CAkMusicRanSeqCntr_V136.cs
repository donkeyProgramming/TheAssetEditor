using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkMusicRanSeqCntr_V136 : HircItem
    {
        public MusicTransNodeParams_V136 MusicTransNodeParams { get; set; } = new MusicTransNodeParams_V136();
        public uint NumPlaylistItems { get; set; }
        public List<AkMusicRanSeqPlaylistItem_V136> PlayList { get; set; } = [];

        protected override void ReadData(ByteChunk chunk)
        {
            MusicTransNodeParams.ReadData(chunk);
            NumPlaylistItems = chunk.ReadUInt32();
            // Playlists work linearly (unlike decision trees):
            // node[0]            ch=3
            //   node[1]          ch=2    // parent: [0]
            //     node[2]        ch=1    // parent: [1]
            //       node[3]      ch=0    // parent: [2]
            //     node[4]        ch=0    // parent: [1]
            //   node[5]          ch=0    // parent: [0]
            //   node[6]          ch=1    // parent: [0]
            //     node[7]        ch=0    // parent: [6]
            PlayList.Add(AkMusicRanSeqPlaylistItem_V136.ReadData(chunk));
        }

        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");

        public class AkMusicRanSeqPlaylistItem_V136
        {
            public uint SegmentId { get; set; }
            public int PlaylistItemId { get; set; }
            public uint NumChildren { get; set; }
            public uint RsType { get; set; }
            public short Loop { get; set; }
            public short LoopMin { get; set; }
            public short LoopMax { get; set; }
            public uint Weight { get; set; }
            public ushort AvoidRepeatCount { get; set; }
            public byte IsUsingWeight { get; set; }
            public byte IsShuffle { get; set; }
            public List<AkMusicRanSeqPlaylistItem_V136> PlayList { get; set; } = [];

            public static AkMusicRanSeqPlaylistItem_V136 ReadData(ByteChunk chunk)
            {
                var akMusicRanSeqPlaylistItem = new AkMusicRanSeqPlaylistItem_V136
                {
                    SegmentId = chunk.ReadUInt32(),
                    PlaylistItemId = chunk.ReadInt32(),
                    NumChildren = chunk.ReadUInt32(),
                    RsType = chunk.ReadUInt32(),
                    Loop = chunk.ReadShort(),
                    LoopMin = chunk.ReadShort(),
                    LoopMax = chunk.ReadShort(),
                    Weight = chunk.ReadUInt32(),
                    AvoidRepeatCount = chunk.ReadUShort(),
                    IsUsingWeight = chunk.ReadByte(),
                    IsShuffle = chunk.ReadByte()
                };

                for (var i = 0; i < akMusicRanSeqPlaylistItem.NumChildren; i++)
                    akMusicRanSeqPlaylistItem.PlayList.Add(ReadData(chunk));

                return akMusicRanSeqPlaylistItem;
            }
        }
    }
}
