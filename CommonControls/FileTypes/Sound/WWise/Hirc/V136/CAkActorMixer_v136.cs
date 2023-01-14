using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    public class CAkActorMixer_v136 : HircItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}

