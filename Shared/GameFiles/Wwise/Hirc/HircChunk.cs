using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircChunk
    {
        public static uint ChunkHeaderSize { get => 4; }
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public uint NumHircItems { get; set; }
        public List<HircItem> HircItems { get; set; } = [];

        public static HircChunk ReadData(string filePath, ByteChunk chunk, uint bankGeneratorVersion, uint languageId, bool isCAHircItem)
        {
            var failedItems = new List<uint>();
            var factory = HircFactory.CreateFactory(bankGeneratorVersion);

            var hircChunk = new HircChunk
            {
                ChunkHeader = ChunkHeader.ReadData(chunk),
                NumHircItems = chunk.ReadUInt32()
            };

            for (uint itemIndex = 0; itemIndex < hircChunk.NumHircItems; itemIndex++)
            {
                var hircType = (AkBkHircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.ByteIndexInFile = itemIndex;
                    hircItem.BnkFilePath = filePath;
                    hircItem.LanguageId = languageId;
                    hircItem.IsCAHircItem = isCAHircItem;
                    hircItem.ReadHirc(chunk);
                    hircChunk.HircItems.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unknownHirc = new UnknownHircItem
                    {
                        ErrorMsg = e.Message,
                        ByteIndexInFile = itemIndex,
                        BnkFilePath = filePath
                    };
                    unknownHirc.ReadHirc(chunk);
                    hircChunk.HircItems.Add(unknownHirc);
                }
            }

            return hircChunk;
        }

        public static byte[] WriteData(HircChunk hircChunk, uint gameBankGeneratorVersion)
        {
            using var memStream = new MemoryStream();
            memStream.Write(ChunkHeader.WriteData(hircChunk.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(hircChunk.NumHircItems, out _));

            foreach (var hircItem in hircChunk.HircItems)
            {
                var bytes = hircItem.WriteData();
                memStream.Write(bytes);
            }

            var byteArray = memStream.ToArray();

            // Reload to ensure sanity
            ReadData("name", new ByteChunk(byteArray), gameBankGeneratorVersion, 0, true);

            return byteArray;
        }
    }
}
