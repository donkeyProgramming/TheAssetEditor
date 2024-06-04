using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise.Hirc.Shared;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkMusicSwitchCntr_v136 : HircItem, INodeBaseParamsAccessor
    {
        public NodeBaseParams NodeBaseParams => MusicTransNodeParams.MusicNodeParams.NodeBaseParams;

        public MusicTransNodeParams MusicTransNodeParams { get; set; }
        public byte bIsContinuePlayback { get; set; }



        public uint uTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint uTreeDataSize { get; set; }
        public byte uMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }


        protected override void CreateSpecificData(ByteChunk chunk)
        {
            MusicTransNodeParams = MusicTransNodeParams.Create(chunk);
            bIsContinuePlayback = chunk.ReadByte();

            uTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, uTreeDepth);
            uTreeDataSize = chunk.ReadUInt32();
            uMode = chunk.ReadByte();
            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}