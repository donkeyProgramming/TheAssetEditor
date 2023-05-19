using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkBus_v136 : HircItem
    {
        public uint OverrideBusId { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            OverrideBusId = chunk.ReadUInt32();
            chunk.ReadBytes((int)Size - 8);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class CAkAuxBus_v136 : HircItem
    {
        public uint OverrideBusId { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            OverrideBusId = chunk.ReadUInt32();
            chunk.ReadBytes((int)Size - 8);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
