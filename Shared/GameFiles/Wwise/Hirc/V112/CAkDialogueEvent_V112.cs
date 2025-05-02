using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkDialogueEvent_V112 : HircItem, ICAkDialogueEvent
    {
        public byte Probability { get; set; }
        public uint TreeDepth { get; set; }
        public List<IAkGameSync> Arguments { get; set; } = [];
        public uint TreeDataSize { get; set; }
        public byte Mode { get; set; }
        public IAkDecisionTree AkDecisionTree { get; set; } = new AkDecisionTree_V112();

        protected override void ReadData(ByteChunk chunk)
        {
            Probability = chunk.ReadByte();

            TreeDepth = chunk.ReadUInt32();
            for (uint i = 0; i < TreeDepth; i++)
                Arguments.Add(new AkGameSync_V112());

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

        public override void UpdateSectionSize() => throw new NotImplementedException();
        public override byte[] WriteData() => throw new NotImplementedException();
    }
}
