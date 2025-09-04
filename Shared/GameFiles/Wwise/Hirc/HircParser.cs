using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircParser
    {
        public static HircChunk Parse(string filePath, ByteChunk chunk, uint bnkVersion, uint languageId, bool isCaHircItem)
        {
            var hircChunk = new HircChunk
            {
                ChunkHeader = BnkChunkHeader.ReadData(chunk),
                NumHircItems = chunk.ReadUInt32()
            };

            var failedItems = new List<uint>();
            var factory = HircFactory.CreateFactory(bnkVersion);

            for (uint itemIndex = 0; itemIndex < hircChunk.NumHircItems; itemIndex++)
            {
                var hircType = (AkBkHircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.ByteIndexInFile = itemIndex;
                    hircItem.OwnerFilePath = filePath;
                    hircItem.LanguageId = languageId;
                    hircItem.IsCaHircItem = isCaHircItem;
                    hircItem.Parse(chunk);
                    hircChunk.HircItems.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unknownHirc = new UnknownHirc
                    {
                        ErrorMsg = e.Message,
                        ByteIndexInFile = itemIndex,
                        OwnerFilePath = filePath
                    };
                    unknownHirc.Parse(chunk);
                    hircChunk.HircItems.Add(unknownHirc);
                }
            }

            return hircChunk;
        }

        public static byte[] WriteData(HircChunk hircChunk, uint gameBankGeneratorVersion)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.WriteData(hircChunk.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(hircChunk.NumHircItems, out _));

            foreach (var hircItem in hircChunk.HircItems)
            {
                var bytes = hircItem.WriteData();
                memStream.Write(bytes);
            }

            var byteArray = memStream.ToArray();

            // For sanity, read back
            Parse("name", new ByteChunk(byteArray), gameBankGeneratorVersion, 0, true);

            return byteArray;
        }
    }
}
