using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Bkhd
{
    public class BkhdParser
    {
        public BkhdHeader Parse(string fileName, ByteChunk chunk)
        {
            var bkdh = new BkhdHeader()
            {
                OwnerFileName = fileName,
                ChunkHeader = BnkChunkHeader.CreateFromBytes(chunk),

                dwBankGeneratorVersion = chunk.ReadUInt32(),
                dwSoundBankId = chunk.ReadUInt32(),
                dwLanguageId = chunk.ReadUInt32(),
                bFeedbackInBank = chunk.ReadUInt32(),
                dwProjectID = chunk.ReadUInt32(),
            };

            // Read the padding
            var headerDiff = (int)bkdh.ChunkHeader.ChunkSize - 20;
            if (headerDiff > 0)
                bkdh.padding = chunk.ReadBytes(headerDiff);

            // Sometimes the version number is strange, probably because CA has their own compiled 
            // version of WWISE. Their version is based on an official version, so we map it to the
            // closest known ID
            if (bkdh.dwBankGeneratorVersion == 2147483770)
                bkdh.dwBankGeneratorVersion = 122;

            if (bkdh.dwBankGeneratorVersion == 2147483784)
                bkdh.dwBankGeneratorVersion = 136;

            return bkdh;
        }

        public static byte[] GetAsByteArray(BkhdHeader header)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(header.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwBankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwSoundBankId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwLanguageId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.bFeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwProjectID, out _));
            memStream.Write(header.padding);
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            parser.Parse("name", new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
