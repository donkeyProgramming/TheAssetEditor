﻿using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise.Hirc.Shared;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkMusicSwitchCntr_v136 : HircItem, INodeBaseParamsAccessor
    {
        public NodeBaseParams NodeBaseParams => MusicTransNodeParams.MusicNodeParams.NodeBaseParams;

        public MusicTransNodeParams MusicTransNodeParams { get; set; }
        public byte BIsContinuePlayback { get; set; }
        public uint UTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint UTreeDataSize { get; set; }
        public byte UMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            MusicTransNodeParams = MusicTransNodeParams.Create(chunk);
            BIsContinuePlayback = chunk.ReadByte();

            UTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, UTreeDepth);
            UTreeDataSize = chunk.ReadUInt32();
            UMode = chunk.ReadByte();
            AkDecisionTree = new AkDecisionTree(chunk, UTreeDepth, UTreeDataSize);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
