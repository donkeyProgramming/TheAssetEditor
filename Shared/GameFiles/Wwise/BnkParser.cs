using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Data;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Stid;

namespace Shared.GameFormats.Wwise
{
    public class BnkParser
    {
        readonly HircParser _hircParser = new();

        public BnkParser()
        {
        }

        public ParsedBnkFile Parse(PackFile file, string fullName, bool isCaHircItem)
        {
            var chunk = file.DataSource.ReadDataAsChunk();
            var parsedBnkFile = new ParsedBnkFile();

            while (chunk.BytesLeft != 0)
            {
                var chunkHeader = BnkChunkHeader.PeekFromBytes(chunk);
                var indexBeforeRead = chunk.Index;
                var expectedIndexAfterRead = indexBeforeRead + BnkChunkHeader.HeaderByteSize + chunkHeader.ChunkSize;

                if (BankChunkTypes.BKHD == chunkHeader.Tag)
                    parsedBnkFile.BkhdChunk = LoadBkhdChunk(fullName, chunk);
                else if (BankChunkTypes.HIRC == chunkHeader.Tag)
                    parsedBnkFile.HircChunk = LoadHircChunk(fullName, chunk, chunkHeader.ChunkSize, parsedBnkFile.BkhdChunk.AkBankHeader.DwBankGeneratorVersion, isCaHircItem);
                else if (BankChunkTypes.DIDX == chunkHeader.Tag)
                    parsedBnkFile.DidxChunk = LoadDidxChunk(fullName, chunk);
                else if (BankChunkTypes.DATA == chunkHeader.Tag)
                    parsedBnkFile.DataChunk = LoadDataChunk(fullName, chunk);
                else if (BankChunkTypes.STID == chunkHeader.Tag)
                    LoadStidChunk(fullName, chunk); // We never care about this. Discard after loading
                else
                    throw new ArgumentException($"Unknown data block '{chunkHeader.Tag}' while parsing bnk file '{fullName}'");

                // Verify
                var bytesRead = expectedIndexAfterRead - indexBeforeRead;
                if (chunk.Index != expectedIndexAfterRead)
                    throw new Exception($"Error parsing bnk with tag '{chunkHeader.Tag}', incorrect num bytes read. '{bytesRead}' bytes read in this operation");
            }

            if (chunk.BytesLeft != 0)
                throw new Exception("Error parsing bnk, bytes left");

            return parsedBnkFile;
        }

        private static BkhdChunk LoadBkhdChunk(string fullName, ByteChunk chunk) => BkhdParser.Parse(fullName, chunk);

        private HircChunk LoadHircChunk(string fullName, ByteChunk chunk, uint chunkHeaderSize, uint bnkVersion, bool isCaHircItem)
        {
            var hircData = _hircParser.Parse(fullName, chunk, bnkVersion, isCaHircItem);

            var hircSizes = hircData.HircItems.Sum(x => x.SectionSize);
            var expectedHircChunkSize = hircSizes + hircData.NumHircItems * 5 + 4;
            var areEqual = expectedHircChunkSize == chunkHeaderSize;
            if (areEqual == false)
                throw new Exception("Error parsing HIRC in bnk, expected and actual not matching");

            return hircData;
        }

        private static DidxChunk LoadDidxChunk(string fullName, ByteChunk chunk) => DidxParser.Parse(fullName, chunk, null);
        private static ByteChunk LoadDataChunk(string fullName, ByteChunk chunk) => DataParser.Parse(fullName, chunk, null);
        static void LoadStidChunk(string fullName, ByteChunk chunk) => StidParser.Parse(fullName, chunk, null);
    }
}
