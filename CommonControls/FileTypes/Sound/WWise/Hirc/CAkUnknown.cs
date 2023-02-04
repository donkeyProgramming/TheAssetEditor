using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;

namespace CommonControls.FileTypes.Sound.WWise.Hirc
{
    public class CAkUnknown : HircItem
    {
        public string ErrorMsg { get; set; }


        protected override void CreateSpesificData(ByteChunk chunk)
        {
            chunk.ReadBytes((int)Size-4);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }


    public class ByteHirc : HircItem
    {
        public byte[] HircData { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            HircData = chunk.ReadBytes((int)Size - 4);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(HircData);

            return memStream.ToArray();
        }
    }
}
