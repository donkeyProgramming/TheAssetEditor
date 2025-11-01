using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Bkhd
{
    public class BkhdChunk
    {
        public string OwnerFilePath { get; set; }
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public AkBankHeader AkBankHeader { get; set; } = new AkBankHeader();

        public static BkhdChunk ReadData(string fileName, ByteChunk chunk)
        {
            var bkdh = new BkhdChunk()
            {
                OwnerFilePath = fileName,
                ChunkHeader = ChunkHeader.ReadData(chunk),
            };
            bkdh.AkBankHeader.ReadData(chunk, bkdh.ChunkHeader.ChunkSize);
            return bkdh;
        }

        public static byte[] WriteData(BkhdChunk bkhdChunk)
        {
            using var memStream = new MemoryStream();
            memStream.Write(ChunkHeader.WriteData(bkhdChunk.ChunkHeader));
            memStream.Write(bkhdChunk.AkBankHeader.WriteData());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            ReadData("name", new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
