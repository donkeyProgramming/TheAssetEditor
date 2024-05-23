using SharedCore.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkRanSeqCntr_v136 : CAkRanSeqCnt, INodeBaseParamsAccessor
    {
        public NodeBaseParams NodeBaseParams { get; set; }

        public ushort sLoopCount { get; set; }
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

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);

            sLoopCount = chunk.ReadUShort();
            sLoopModMin = chunk.ReadUShort();
            sLoopModMax = chunk.ReadUShort();

            fTransitionTime = chunk.ReadSingle();
            fTransitionTimeModMin = chunk.ReadSingle();
            fTransitionTimeModMax = chunk.ReadSingle();

            wAvoidRepeatCount = chunk.ReadUShort();

            eTransitionMode = chunk.ReadByte();
            eRandomMode = chunk.ReadByte();
            eMode = chunk.ReadByte();
            byBitVector = chunk.ReadByte();

            Children = Children.Create(chunk);

            var playListItemCount = chunk.ReadUShort();
            for (int i = 0; i < playListItemCount; i++)
                AkPlaylist.Add(AkPlaylistItem.Create(chunk));
        }

        public override uint GetParentId() => NodeBaseParams.DirectParentID;
        public override List<uint> GetChildren() => AkPlaylist.Select(x => x.PlayId).ToList();

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
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

