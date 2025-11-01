using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public partial class CAkDialogueEvent_V136 : HircItem, ICAkDialogueEvent
    {
        public byte Probability { get; set; }
        public uint TreeDepth { get; set; }
        public List<IAkGameSync> Arguments { get; set; } = [];
        public uint TreeDataSize { get; set; }
        public byte Mode { get; set; }
        public IAkDecisionTree AkDecisionTree { get; set; } = new AkDecisionTree_V136();
        public AkPropBundle_V136 AkPropBundle0 { get; set; } = new AkPropBundle_V136();
        public AkPropBundleMinMax_V136 AkPropBundle1 { get; set; } = new AkPropBundleMinMax_V136();

        protected override void ReadData(ByteChunk chunk)
        {
            Probability = chunk.ReadByte();

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
            AkPropBundle0.ReadData(chunk);
            AkPropBundle1.ReadData(chunk);
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(Probability, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(TreeDepth, out _));

            // Write all the IDs first
            for (var i = 0; i < TreeDepth; i++)
                memStream.Write(ByteParsers.UInt32.EncodeValue(Arguments[i].GroupId, out _));

            // Then write all the values
            for (var i = 0; i < TreeDepth; i++)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)Arguments[i].GroupType, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue(TreeDataSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(Mode, out _));
            memStream.Write(AkDecisionTree.WriteData());
            memStream.Write(AkPropBundle0.WriteData());
            memStream.Write(AkPropBundle1.WriteData());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkDialogueEvent_V136();
            sanityReload.ReadHirc(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            var probabilitySize = ByteHelper.GetPropertyTypeSize(Probability);
            var treeDepthSize = ByteHelper.GetPropertyTypeSize(TreeDepth);

            uint arugumentsSize = 0;
            foreach (var argument in Arguments)
                arugumentsSize += argument.GetSize();

            var treeDataSizeSize = ByteHelper.GetPropertyTypeSize(TreeDataSize);
            var modeSize = ByteHelper.GetPropertyTypeSize(Mode);
            SectionSize = idSize + probabilitySize + treeDepthSize + arugumentsSize + treeDataSizeSize + modeSize + TreeDataSize + AkPropBundle0.GetSize() + AkPropBundle1.GetSize();
        }

        public CAkDialogueEvent_V136 Clone()
        {
            return new CAkDialogueEvent_V136
            {
                LanguageId = LanguageId,
                HircType = HircType,
                SectionSize = SectionSize,
                Id = Id,
                Probability = Probability,
                TreeDepth = TreeDepth,
                TreeDataSize = TreeDataSize,
                Mode = Mode,
                Arguments = Arguments.Select(argument => argument is AkGameSync_V136 gameSync ? gameSync.Clone() : argument).Cast<IAkGameSync>().ToList(),
                AkDecisionTree = AkDecisionTree is AkDecisionTree_V136 decisionTree ? decisionTree.Clone() : AkDecisionTree,
                AkPropBundle0 = AkPropBundle0.Clone(),
                AkPropBundle1 = AkPropBundle1.Clone()
            };
        }
    }
}
