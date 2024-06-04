using System;
using System.Collections.Generic;
using Shared.Core.ByteParsing;

namespace Audio.FileFormats.WWise.Hirc.V112
{
    public class CAkActorMixer_v112 : HircItem, ICAkActorMixer
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);
        }

        public override void UpdateSize() => throw new NotImplementedException();

        public override byte[] GetAsByteArray() => throw new NotImplementedException();

        public List<uint> GetChildren() => Children.ChildIdList;
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
    }
}
