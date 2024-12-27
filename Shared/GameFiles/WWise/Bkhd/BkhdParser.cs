using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Bkhd
{
    public class BkhdParser
    {
        public static BkhdHeader Parse(string fileName, ByteChunk chunk)
        {
            var bkdh = new BkhdHeader()
            {
                OwnerFileName = fileName,
                ChunkHeader = BnkChunkHeader.CreateFromBytes(chunk),

                DwBankGeneratorVersion = chunk.ReadUInt32(),
                DwSoundBankId = chunk.ReadUInt32(),
                DwLanguageId = chunk.ReadUInt32(),
                BFeedbackInBank = chunk.ReadUInt32(),
                DwProjectId = chunk.ReadUInt32(),
            };

            // Read the padding
            var headerDiff = (int)bkdh.ChunkHeader.ChunkSize - 20;
            if (headerDiff > 0)
                bkdh.Padding = chunk.ReadBytes(headerDiff);

            // Sometimes the version number is strange, probably because CA has their own compiled 
            // version of WWISE. Their version is based on an official version, so we map it to the
            // closest known ID
            if (bkdh.DwBankGeneratorVersion == 2147483770)
                bkdh.DwBankGeneratorVersion = 122;

            if (bkdh.DwBankGeneratorVersion == 2147483784)
                bkdh.DwBankGeneratorVersion = 136;

            return bkdh;
        }

        public static byte[] GetAsByteArray(BkhdHeader header)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(header.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.DwBankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.DwSoundBankId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.DwLanguageId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.BFeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.DwProjectId, out _));
            memStream.Write(header.Padding);
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            Parse("name", new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
