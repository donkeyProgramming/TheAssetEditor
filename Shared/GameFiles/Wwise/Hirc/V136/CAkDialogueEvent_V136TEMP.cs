using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public partial class CAkDialogueEvent_V136TEMP : HircItem, ICAkDialogueEvent
    {
        public byte Probability { get; set; }
        public uint TreeDepth { get; set; }
        public List<AkGameSync_V136> Arguments { get; set; } = [];
        public uint TreeDataSize { get; set; }
        public byte Mode { get; set; }
        public AkDecisionTree_V136 AkDecisionTree { get; set; } = new AkDecisionTree_V136();
        public AkPropBundle_V136 AkPropBundle0 { get; set; } = new AkPropBundle_V136();
        public AkPropBundleMinMax_V136 AkPropBundle1 { get; set; } = new AkPropBundleMinMax_V136();

        protected override void CreateSpecificData(ByteChunk chunk)
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
            AkDecisionTree.CreateSpecificData(chunk, TreeDataSize, TreeDepth);
            AkPropBundle0.CreateSpecificData(chunk);
            AkPropBundle1.CreateSpecificData(chunk);
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(Probability, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(TreeDepth, out _));

            // Write all the Ids first
            for (var i = 0; i < TreeDepth; i++)
                memStream.Write(ByteParsers.UInt32.EncodeValue(Arguments[i].GroupId, out _));

            // Then write all the values
            for (var i = 0; i < TreeDepth; i++)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)Arguments[i].GroupType, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue(TreeDataSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(Mode, out _));
            memStream.Write(AkDecisionTree.GetAsByteArray());
            memStream.Write(AkPropBundle0.GetAsByteArray());
            memStream.Write(AkPropBundle1.GetAsByteArray());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkDialogueEvent_V136TEMP();
            sanityReload.Parse(new ByteChunk(byteArray));

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

        List<object> ICAkDialogueEvent.Arguments => Arguments.Cast<object>().ToList();
        object ICAkDialogueEvent.AkDecisionTree => AkDecisionTree;
    }
}
