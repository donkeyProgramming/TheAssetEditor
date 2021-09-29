using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Stid
{
    public class StidParser : IParser
    {
       
        public void Parse(string fileName, ByteChunk chunk, SoundDataBase soundDb)
        {
            var size = chunk.ReadUInt32();
            chunk.Index += (int)size;
        }
    }
}
