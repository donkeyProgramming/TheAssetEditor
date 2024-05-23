using Audio.FileFormats.WWise.Hirc.Shared;
using SharedCore.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc.V122
{
    public class CAkDialogueEvent_v122 : HircItem, ICADialogEvent
    {

        public byte uProbability { get; set; }
        public uint uTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint uTreeDataSize { get; set; }
        public byte uMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            uProbability = chunk.ReadByte();
            uTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, uTreeDepth);
            uTreeDataSize = chunk.ReadUInt32();
            uMode = chunk.ReadByte();
            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, Size);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
