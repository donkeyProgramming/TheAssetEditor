﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc
{
    public class CAkUnknown : HircItem
    {
        public string ErrorMsg { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            chunk.ReadBytes((int)Size - 4);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
