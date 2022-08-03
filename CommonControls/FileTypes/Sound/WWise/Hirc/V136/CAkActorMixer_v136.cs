using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    class CAkActorMixer_v136 : HircItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        protected override void CreateSpesificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);
        }
    }
}

