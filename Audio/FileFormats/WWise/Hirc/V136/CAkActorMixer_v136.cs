using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audio.FileFormats.WWise.Hirc.V136
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

        public override void UpdateSize()
        {
            Size = BnkChunkHeader.HeaderByteSize + Children.GetSize() + NodeBaseParams.GetSize()+1;
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.GetAsByteArray());
            memStream.Write(Children.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkActorMixer_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
        }
    }
}

