using Shared.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkMusicSwitchCntr_V136 : HircItem
    {
        public MusicTransNodeParams_V136 MusicTransNodeParams { get; set; } = new MusicTransNodeParams_V136();
        public byte IsContinuePlayback { get; set; }
        public uint TreeDepth { get; set; }
        public List<AkGameSync_V136> Arguments { get; set; } = [];
        public uint TreeDataSize { get; set; }
        public byte Mode { get; set; }
        public AkDecisionTree_V136 AkDecisionTree { get; set; } = new AkDecisionTree_V136();

        protected override void ReadData(ByteChunk chunk)
        {
            MusicTransNodeParams.ReadData(chunk);
            IsContinuePlayback = chunk.ReadByte();

            TreeDepth = chunk.ReadUInt32();
            for (uint i = 0; i < TreeDepth; i++)
                Arguments.Add(new AkGameSync_V136());

            // First read all the group ids
            for (var i = 0; i < TreeDepth; i++)
                Arguments[i].GroupId = chunk.ReadUInt32();

            // Then read all the group types
            for (var i = 0; i < TreeDepth; i++)
                Arguments[i].GroupType = (AkGroupType)chunk.ReadByte();

            TreeDataSize = chunk.ReadUInt32();
            Mode = chunk.ReadByte();
            AkDecisionTree.ReadData(chunk, TreeDataSize, TreeDepth);
        }

        public override byte[] WriteData()
        {
            var memStream = WriteHeader();
            memStream.Write(MusicTransNodeParams.WriteData());
            memStream.Write(ByteParsers.Byte.EncodeValue(IsContinuePlayback, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)Arguments.Count, out _));

            // The arguments are stored as two parallel runs - every group id, then every group
            // type - rather than interleaved per argument.
            foreach (var argument in Arguments)
                memStream.Write(ByteParsers.UInt32.EncodeValue(argument.GroupId, out _));

            foreach (var argument in Arguments)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)argument.GroupType, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue(AkDecisionTree.GetSize(), out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(Mode, out _));
            memStream.Write(AkDecisionTree.WriteData());
            return memStream.ToArray();
        }

        public override void UpdateSectionSize()
        {
            var size = MusicTransNodeParams.GetSize()
                + 1 // IsContinuePlayback
                + 4 // TreeDepth
                + (uint)Arguments.Sum(argument => argument.GetSize())
                + 4 // TreeDataSize
                + 1 // Mode
                + AkDecisionTree.GetSize();

            SectionSize = size + ByteHelper.GetPropertyTypeSize(Id);
        }
    }
}
