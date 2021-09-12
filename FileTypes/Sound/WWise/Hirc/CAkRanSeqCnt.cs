using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
{

    public class CAkRanSeqCnt : HricItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }
 
        public ushort LoopCount { get; set; }
        public ushort sLoopModMin { get; set; }
        public ushort sLoopModMax { get; set; }
        public float fTransitionTime { get; set; }
        public float fTransitionTimeModMin { get; set; }
        public float fTransitionTimeModMax { get; set; }
        public ushort wAvoidRepeatCount { get; set; }
        public byte eTransitionMode { get; set; }
        public byte eRandomMode { get; set; }
        public byte eMode { get; set; }
        public byte byBitVector { get; set; }

        public Children Children { get; set; }
        public List<AkPlaylistItem> AkPlaylist { get; set; } = new List<AkPlaylistItem>();

        // Playlist

        public static CAkRanSeqCnt Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var ranSeqCnt = new CAkRanSeqCnt();
            ranSeqCnt.LoadCommon(chunk);
            ranSeqCnt.NodeBaseParams = NodeBaseParams.Create(chunk);

            ranSeqCnt.LoopCount = chunk.ReadUShort();
            ranSeqCnt.sLoopModMin = chunk.ReadUShort();
            ranSeqCnt.sLoopModMax = chunk.ReadUShort();

            ranSeqCnt.fTransitionTime = chunk.ReadSingle();
            ranSeqCnt.fTransitionTimeModMin = chunk.ReadSingle();
            ranSeqCnt.fTransitionTimeModMax = chunk.ReadSingle();

            ranSeqCnt.wAvoidRepeatCount = chunk.ReadUShort();

            ranSeqCnt.eTransitionMode = chunk.ReadByte();
            ranSeqCnt.eRandomMode = chunk.ReadByte();
            ranSeqCnt.eMode = chunk.ReadByte();
            ranSeqCnt.byBitVector = chunk.ReadByte();

            ranSeqCnt.Children = Children.Create(chunk);

            var playListItemCount = chunk.ReadUShort();
            for (int i = 0; i < playListItemCount; i++)
                ranSeqCnt.AkPlaylist.Add(AkPlaylistItem.Create(chunk));

            ranSeqCnt.SkipToEnd(chunk, objectStartIndex + 5);
            return ranSeqCnt;
        }
    }


    public class AkPlaylistItem
    {
        public uint PlayId { get; set; }
        public int Weight { get; set; } 

        public static AkPlaylistItem Create(ByteChunk chunk)
        {
            var instance = new AkPlaylistItem();
            instance.PlayId = chunk.ReadUInt32();
            instance.Weight = chunk.ReadInt32();
            return instance;
        }
    }
}
