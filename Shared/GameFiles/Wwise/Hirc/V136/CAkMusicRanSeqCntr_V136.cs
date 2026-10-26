using Shared.ByteParsing;
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
            //
            // NumPlaylistItems counts every node in that flat run, not just the roots, so reading
            // the root recursively consumes exactly the whole run. A container with no playlist at
            // all has no root to read.
            if (NumPlaylistItems > 0)
                PlayList.Add(AkMusicRanSeqPlaylistItem_V136.ReadData(chunk));
        }

        public override byte[] WriteData()
        {
            var memStream = WriteHeader();
            memStream.Write(MusicTransNodeParams.WriteData());
            memStream.Write(ByteParsers.UInt32.EncodeValue(CountPlaylistItems(), out _));
            foreach (var playlistItem in PlayList)
                memStream.Write(playlistItem.WriteData());
            return memStream.ToArray();
        }

        public override void UpdateSectionSize()
        {
            var size = MusicTransNodeParams.GetSize()
                + 4 // NumPlaylistItems
                + CountPlaylistItems() * AkMusicRanSeqPlaylistItem_V136.Size;

            SectionSize = size + ByteHelper.GetPropertyTypeSize(Id);
        }

        // Every node in the flattened run, not just the roots - see ReadData.
        private uint CountPlaylistItems() => (uint)PlayList.Sum(playlistItem => playlistItem.CountSelfAndDescendants());

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

            // 4 + 4 + 4 + 4 + 2 + 2 + 2 + 4 + 2 + 1 + 1, children excluded
            public const uint Size = 30;

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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(SegmentId, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(PlaylistItemId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)PlayList.Count, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(RsType, out _));
                memStream.Write(ByteParsers.Short.EncodeValue(Loop, out _));
                memStream.Write(ByteParsers.Short.EncodeValue(LoopMin, out _));
                memStream.Write(ByteParsers.Short.EncodeValue(LoopMax, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(Weight, out _));
                memStream.Write(ByteParsers.UShort.EncodeValue(AvoidRepeatCount, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(IsUsingWeight, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(IsShuffle, out _));

                foreach (var child in PlayList)
                    memStream.Write(child.WriteData());

                return memStream.ToArray();
            }

            public uint CountSelfAndDescendants() => 1 + (uint)PlayList.Sum(child => child.CountSelfAndDescendants());
        }
    }
}
