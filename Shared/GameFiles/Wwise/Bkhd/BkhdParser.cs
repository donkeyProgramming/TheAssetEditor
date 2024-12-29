using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Bkhd
{
    public class BkhdParser
    {
        public static BkhdChunk Parse(string fileName, ByteChunk chunk)
        {
            var bkdh = new BkhdChunk()
            {
                OwnerFile = fileName,
                ChunkHeader = BnkChunkHeader.CreateSpecificData(chunk),
                AkBankHeader = new AkBankHeader()
            };
            bkdh.AkBankHeader.CreateSpecificData(chunk, bkdh.ChunkHeader.ChunkSize);

            // Sometimes the version number is strange, probably because CA has their own compiled 
            // version of WWISE. Their version is based on an official version, so we map it to the
            // closest known ID
            if (bkdh.AkBankHeader.DwBankGeneratorVersion == 2147483770)
                bkdh.AkBankHeader.DwBankGeneratorVersion = 122;

            if (bkdh.AkBankHeader.DwBankGeneratorVersion == 2147483784)
                bkdh.AkBankHeader.DwBankGeneratorVersion = 136;

            return bkdh;
        }

        public static byte[] GetAsByteArray(BkhdChunk header)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(header.ChunkHeader));
            memStream.Write(header.AkBankHeader.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            Parse("name", new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
