using Shared.Core.ByteParsing;
namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkRanSeqCntr_v136 : HircItem, ICAkRanSeqCnt
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public ushort SLoopCount { get; set; }
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

            SLoopCount = chunk.ReadUShort();
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

        public override void UpdateSectionSize()
        {
            var nodeBaseParams = NodeBaseParams.GetSize();
            var children = Children.GetSize();
            var akPlaylistCount = Convert.ToUInt32(AkPlaylist.Count);

            SectionSize = BnkChunkHeader.HeaderByteSize + nodeBaseParams + 2 + 2 + 2 + 4 + 4 + 4 + 2 + 1 + 1 + 1 + 1 + children + 2 + akPlaylistCount * 8 - 4;
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.GetAsByteArray());

            memStream.Write(ByteParsers.UShort.EncodeValue(SLoopCount, out _));
            memStream.Write(ByteParsers.UShort.EncodeValue(SLoopModMin, out _));
            memStream.Write(ByteParsers.UShort.EncodeValue(SLoopModMax, out _));

            memStream.Write(ByteParsers.Single.EncodeValue(FTransitionTime, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(FTransitionTimeModMin, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(FTransitionTimeModMax, out _));

            memStream.Write(ByteParsers.UShort.EncodeValue(WAvoidRepeatCount, out _));

            memStream.Write(ByteParsers.Byte.EncodeValue(ETransitionMode, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(ERandomMode, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(EMode, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(ByBitVector, out _));

            memStream.Write(Children.GetAsByteArray());

            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)AkPlaylist.Count(), out _));
            foreach (var akPlaylistItem in AkPlaylist)
            {
                memStream.Write(ByteParsers.UInt32.EncodeValue(akPlaylistItem.PlayId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(Convert.ToUInt32(akPlaylistItem.Weight), out _));
            }

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkRanSeqCntr_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
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

