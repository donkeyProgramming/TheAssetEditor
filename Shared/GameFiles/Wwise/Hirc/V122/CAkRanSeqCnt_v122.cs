using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V122
{
    public class CAkRanSeqCnt_V122 : HircItem, ICAkRanSeqCnt
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public ushort LoopCount { get; set; }
        public ushort SLoopModMin { get; set; }
        public ushort SLoopModMax { get; set; }
        public float FTransitionTime { get; set; }
        public float FTransitionTimeModMin { get; set; }
        public float FTransitionTimeModMax { get; set; }
        public ushort WAvoidRepeatCount { get; set; }
        public byte ETransitionMode { get; set; }
        public byte ERandomMode { get; set; }
        public byte EMode { get; set; }
        public byte ByBitVector { get; set; }
        public Children Children { get; set; }
        public List<AkPlaylistItem> AkPlaylist { get; set; } = [];

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);

            LoopCount = chunk.ReadUShort();
            SLoopModMin = chunk.ReadUShort();
            SLoopModMax = chunk.ReadUShort();

            FTransitionTime = chunk.ReadSingle();
            FTransitionTimeModMin = chunk.ReadSingle();
            FTransitionTimeModMax = chunk.ReadSingle();

            WAvoidRepeatCount = chunk.ReadUShort();

            ETransitionMode = chunk.ReadByte();
            ERandomMode = chunk.ReadByte();
            EMode = chunk.ReadByte();
            ByBitVector = chunk.ReadByte();

            Children = Children.Create(chunk);

            var playListItemCount = chunk.ReadUShort();
            for (var i = 0; i < playListItemCount; i++)
                AkPlaylist.Add(AkPlaylistItem.Create(chunk));
        }

        public uint GetParentId() => NodeBaseParams.DirectParentId;
        public List<uint> GetChildren() => AkPlaylist.Select(x => x.PlayId).ToList();
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
