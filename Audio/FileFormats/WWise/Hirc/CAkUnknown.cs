using Filetypes.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc
{
    public class CAkUnknown : HircItem
    {
        public string ErrorMsg { get; set; }


        protected override void CreateSpesificData(ByteChunk chunk)
        {
            chunk.ReadBytes((int)Size - 4);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
