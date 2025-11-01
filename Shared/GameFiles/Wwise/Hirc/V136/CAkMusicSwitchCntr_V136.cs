using Shared.Core.ByteParsing;
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

        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");
    }
}
