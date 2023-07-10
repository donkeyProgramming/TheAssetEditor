using Audio.FileFormats.WWise.Hirc.Shared;
using Filetypes.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc.V136
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
        public Shared.AkDecisionTree AkDecisionTree { get; set; }


        protected override void CreateSpesificData(ByteChunk chunk)
        {
            MusicTransNodeParams = MusicTransNodeParams.Create(chunk);
            bIsContinuePlayback = chunk.ReadByte();

            uTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, uTreeDepth);
            uTreeDataSize = chunk.ReadUInt32();
            uMode = chunk.ReadByte();
            AkDecisionTree = new Shared.AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}