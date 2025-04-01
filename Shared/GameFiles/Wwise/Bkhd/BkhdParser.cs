using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Bkhd
{
    public class BkhdParser
    {
        public static BkhdChunk Parse(string fileName, ByteChunk chunk)
        {
            var bkdh = new BkhdChunk()
            {
                OwnerFilePath = fileName,
                ChunkHeader = BnkChunkHeader.ReadData(chunk),
            };
            bkdh.AkBankHeader.ReadData(chunk, bkdh.ChunkHeader.ChunkSize);
            return bkdh;
        }

        public static byte[] WriteData(BkhdChunk header)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.WriteData(header.ChunkHeader));
            memStream.Write(header.AkBankHeader.WriteData());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            Parse("name", new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
