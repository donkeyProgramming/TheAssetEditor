using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Data;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Stid;

namespace Shared.GameFormats.Wwise
{
    public class BnkParser
    {
        public static ParsedBnkFile Parse(PackFile packFile, string filePath, bool isCAHircItem)
        {
            var parsedBnkFile = new ParsedBnkFile();
            var chunk = packFile.DataSource.ReadDataAsChunk();

            while (chunk.BytesLeft != 0)
            {
                if (packFile.Name == "init.bnk")
                    continue;

                var chunkHeader = ChunkHeader.PeekFromBytes(chunk);
                var indexBeforeRead = chunk.Index;
                var expectedIndexAfterRead = indexBeforeRead + ChunkHeader.ChunkHeaderSize + chunkHeader.ChunkSize;

                if (BankChunkTypes.BKHD == chunkHeader.Tag)
                    parsedBnkFile.BkhdChunk = LoadBkhdChunk(filePath, chunk);
                else if (BankChunkTypes.HIRC == chunkHeader.Tag)
                    parsedBnkFile.HircChunk = LoadHircChunk(filePath, chunk, chunkHeader.ChunkSize, parsedBnkFile.BkhdChunk.AkBankHeader, isCAHircItem);
                else if (BankChunkTypes.DIDX == chunkHeader.Tag)
                    parsedBnkFile.DidxChunk = LoadDidxChunk(filePath, chunk);
                else if (BankChunkTypes.DATA == chunkHeader.Tag)
                    parsedBnkFile.DataChunk = LoadDataChunk(filePath, chunk);
                else if (BankChunkTypes.STID == chunkHeader.Tag)
                    LoadStidChunk(filePath, chunk); 
                else
                    throw new ArgumentException($"Unknown data block '{chunkHeader.Tag}' while parsing bnk file '{filePath}'");

                // Verify
                var bytesRead = expectedIndexAfterRead - indexBeforeRead;
                if (chunk.Index != expectedIndexAfterRead)
                    throw new Exception($"Error parsing bnk with tag '{chunkHeader.Tag}', incorrect num bytes read. '{bytesRead}' bytes read in this operation");
            }

            if (chunk.BytesLeft != 0)
                throw new Exception("Error parsing bnk, bytes left");

            return parsedBnkFile;
        }

        private static BkhdChunk LoadBkhdChunk(string fullName, ByteChunk chunk) => BkhdChunk.ReadData(fullName, chunk);

        private static HircChunk LoadHircChunk(string fullName, ByteChunk chunk, uint chunkSize, AkBankHeader akBankHeader, bool isCAHircItem)
        {
            var bankGeneratorVersion = akBankHeader.BankGeneratorVersion;
            var languageId = akBankHeader.LanguageId;
            var hircData = HircChunk.ReadData(fullName, chunk, bankGeneratorVersion, languageId, isCAHircItem);

            var expectedHircChunkSize = HircChunk.ChunkHeaderSize + (hircData.HircItems.Sum(hirc => HircItem.HircHeaderSize + hirc.SectionSize));
            var areEqual = expectedHircChunkSize == chunkSize;
            if (areEqual == false)
                throw new Exception("Error parsing HIRC in bnk, expected and actual not matching");

            return hircData;
        }

        private static DidxChunk LoadDidxChunk(string fullName, ByteChunk chunk) => DidxChunk.ReadData(fullName, chunk);
        private static ByteChunk LoadDataChunk(string fullName, ByteChunk chunk) => DataChunk.ReadData(fullName, chunk);
        private static void LoadStidChunk(string fullName, ByteChunk chunk) => StidChunk.ReadData(fullName, chunk);
    }
}
