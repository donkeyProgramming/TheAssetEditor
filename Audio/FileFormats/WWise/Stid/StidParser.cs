using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audio.FileFormats.WWise.Stid
{
    public class StidParser : IParser
    {

        public void Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb)
        {
            var chunckHeader = BnkChunkHeader.CreateFromBytes(chunk);
            chunk.Index += (int)chunckHeader.ChunkSize;
        }
    }
}
