using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Stid
{
    public class StidParser : IParser
    {

        public void Parse(string fileName, ByteChunk chunk, SoundDataBase soundDb)
        {
            var chunckHeader = BnkChunkHeader.CreateFromBytes(chunk);
            chunk.Index += (int)chunckHeader.ChunkSize;
        }
    }
}
