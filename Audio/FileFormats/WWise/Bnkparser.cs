using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Data;
using Audio.FileFormats.WWise.Didx;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Stid;
using CommonControls.FileTypes.PackFiles.Models;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audio.FileFormats.WWise
{
    interface IParser
    {
        void Parse(string fileName, ByteChunk chunk, ParsedBnkFile soundDb);
    }

    public class Bnkparser
    {
        Dictionary<string, IParser> _parsers = new Dictionary<string, IParser>();
        public Bnkparser()
        {
            _parsers["BKHD"] = new BkhdParser();
            _parsers["HIRC"] = new HircParser();
            _parsers["STID"] = new StidParser();
            _parsers["DIDX"] = new DidxParser();
            _parsers["DATA"] = new DataParser();
        }

        public void UseHircByteFactory(bool value) => (_parsers["HIRC"] as HircParser).UseByteFactory = value;

        public ParsedBnkFile Parse(PackFile file, string fullName)
        {
            var chunk = file.DataSource.ReadDataAsChunk();
            var bnkFile = new ParsedBnkFile();

            while (chunk.BytesLeft != 0)
            {
                var chunckHeader = BnkChunkHeader.PeakFromBytes(chunk);
                var indexBeforeRead = chunk.Index;
                var expectedIndexAfterRead = indexBeforeRead + BnkChunkHeader.HeaderByteSize + chunckHeader.ChunkSize;
                _parsers[chunckHeader.Tag].Parse(fullName, chunk, bnkFile);

                var headerTag = chunckHeader.Tag;
                var hircSizes = bnkFile.HircChuck.Hircs.Sum(x => x.Size);
                var bytesRead = expectedIndexAfterRead - indexBeforeRead;
                var expectedHircChuckSize = hircSizes + bnkFile.HircChuck.NumHircItems * 5 + 4;
                var areEqual = expectedHircChuckSize == chunckHeader.ChunkSize;

                if (headerTag == "HIRC")
                {
                    if (areEqual == false)
                        throw new Exception("Error parsing bnk, expected and actuall not matching");
                }

                // Verify index
                if (chunk.Index != expectedIndexAfterRead)
                    throw new Exception($"Error parsing bnk with tag '{headerTag}', incorrect num bytes read");
            }

            if (chunk.BytesLeft != 0)
                throw new Exception("Error parsing bnk, bytes left");

            return bnkFile;
        }
    }
}

//https://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#DIDX_section
//https://github.com/bnnm/wwiser/blob/cd5c086ef2c104e7133e361d385a1023408fb92f/wwiser/wmodel.py#L205
//https://github.com/Maddoxkkm/bnk-event-extraction
//https://github.com/vgmstream/vgmstream/blob/37cc12295c92ec6aa874118fb237bd3821970836/src/meta/bkhd.c
// https://github.com/admiralnelson/total-warhammer-RE-audio/blob/master/BnkExtract.py
// https://github.com/eXpl0it3r/bnkextr