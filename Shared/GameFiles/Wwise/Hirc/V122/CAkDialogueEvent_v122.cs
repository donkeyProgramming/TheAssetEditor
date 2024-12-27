using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V122
{
    public class CAkDialogueEvent_v122 : HircItem, ICAkDialogueEvent
    {
        public byte UProbability { get; set; }
        public uint UTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint UTreeDataSize { get; set; }
        public byte UMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            UProbability = chunk.ReadByte();
            UTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, UTreeDepth);
            UTreeDataSize = chunk.ReadUInt32();
            UMode = chunk.ReadByte();
            AkDecisionTree = new AkDecisionTree(chunk, UTreeDepth, Size);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
