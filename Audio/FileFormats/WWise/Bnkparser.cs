using System;
using System.Linq;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Data;
using Audio.FileFormats.WWise.Didx;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Stid;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Audio.FileFormats.WWise
{
    public class BnkParser
    {
        readonly BkhdParser _bkhdParser = new();
        readonly HircParser _hircParser = new();
        readonly StidParser _stidParser = new();
        readonly DidxParser _didxParser = new();
        readonly DataParser _dataParser = new();

        public BnkParser()
        {
        }

        public ParsedBnkFile Parse(PackFile file, string fullName)
        {
            var chunk = file.DataSource.ReadDataAsChunk();
            var parsedBnkFile = new ParsedBnkFile();

            while (chunk.BytesLeft != 0)
            {
                var chunckHeader = BnkChunkHeader.PeakFromBytes(chunk);
                var indexBeforeRead = chunk.Index;
                var expectedIndexAfterRead = indexBeforeRead + BnkChunkHeader.HeaderByteSize + chunckHeader.ChunkSize;

                if (WWiseObjectHeaders.BKHD == chunckHeader.Tag) 
                    parsedBnkFile.Header = LoadHeader(fullName, chunk);
                else if (WWiseObjectHeaders.HIRC == chunckHeader.Tag) 
                    parsedBnkFile.HircChuck = LoadHircs(fullName, chunk, chunckHeader.ChunkSize, parsedBnkFile.Header.dwBankGeneratorVersion);
                else if (WWiseObjectHeaders.DIDX == chunckHeader.Tag)
                    parsedBnkFile.DidxChunk = LoadDidx(fullName, chunk);  
                else if (WWiseObjectHeaders.DATA == chunckHeader.Tag)
                    parsedBnkFile.DataChunk = LoadData(fullName, chunk);
                else if (WWiseObjectHeaders.STID == chunckHeader.Tag)
                    LoadStid(fullName, chunk);  // We never care about this. Discard after loading
                else
                    throw new ArgumentException($"Unknown data block '{chunckHeader.Tag}' while parsing bnk file '{fullName}'");
 
                // Verify
                var bytesRead = expectedIndexAfterRead - indexBeforeRead;
                if (chunk.Index != expectedIndexAfterRead)
                    throw new Exception($"Error parsing bnk with tag '{chunckHeader.Tag}', incorrect num bytes read. '{bytesRead}' bytes read in this operation");
            }

            if (chunk.BytesLeft != 0)
                throw new Exception("Error parsing bnk, bytes left");

            return parsedBnkFile;
        }

        BkhdHeader LoadHeader(string fullName, ByteChunk chunk) => _bkhdParser.Parse(fullName, chunk);

        HircChunk LoadHircs(string fullName, ByteChunk chunk, uint chunkHeaderSize, uint bnkVersion)
        {
            var hircData = _hircParser.Parse(fullName, chunk, bnkVersion);

            var hircSizes = hircData.Hircs.Sum(x => x.Size);
            var expectedHircChuckSize = hircSizes + hircData.NumHircItems * 5 + 4;
            var areEqual = expectedHircChuckSize == chunkHeaderSize;
            if (areEqual == false)
                throw new Exception("Error parsing HIRC in bnk, expected and actual not matching");

            return hircData;
        }

        DidxChunk LoadDidx(string fullName, ByteChunk chunk) => _didxParser.Parse(fullName, chunk, null);
        ByteChunk LoadData(string fullName, ByteChunk chunk) => _dataParser.Parse(fullName, chunk, null);
        void LoadStid(string fullName, ByteChunk chunk) => _stidParser.Parse(fullName, chunk, null);
    }
}

// https://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#DIDX_section
// https://github.com/bnnm/wwiser/blob/cd5c086ef2c104e7133e361d385a1023408fb92f/wwiser/wmodel.py#L205
// https://github.com/Maddoxkkm/bnk-event-extraction
// https://github.com/vgmstream/vgmstream/blob/37cc12295c92ec6aa874118fb237bd3821970836/src/meta/bkhd.c
// https://github.com/admiralnelson/total-warhammer-RE-audio/blob/master/BnkExtract.py
// https://github.com/eXpl0it3r/bnkextr
