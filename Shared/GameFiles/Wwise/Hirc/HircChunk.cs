using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircChunk
    {
        public static uint ChunkHeaderSize { get => 4; }
        public ChunkHeader ChunkHeader { get; set; } = new ChunkHeader();
        public uint NumHircItems { get; set; }
        public List<HircItem> HircItems { get; set; } = [];

        public static HircChunk ReadData(string filePath, ByteChunk chunk, uint bankGeneratorVersion, uint languageId, bool isCA)
        {
            var hircChunk = new HircChunk
            {
                ChunkHeader = ChunkHeader.ReadData(chunk),
                NumHircItems = chunk.ReadUInt32()
            };

            for (uint itemIndex = 0; itemIndex < hircChunk.NumHircItems; itemIndex++)
                hircChunk.HircItems.Add(
                    HircItem.ReadData(
                        filePath,
                        chunk,
                        bankGeneratorVersion,
                        languageId,
                        isCA,
                        itemIndex));

            var expectedChunkSize = ChunkHeaderSize + hircChunk.HircItems.Sum(hirc => HircHeader.PrefixSize + hirc.SectionSize);
            if (expectedChunkSize != hircChunk.ChunkHeader.ChunkSize)
                throw new Exception("Error parsing HIRC in bnk, expected and actual not matching");

            return hircChunk;
        }

        public static List<HircIndexEntry> BuildIndex(long payloadOffset, uint chunkSize, ByteChunk chunk)
        {
            if (chunkSize < sizeof(uint))
                throw new InvalidDataException($"HIRC chunk is only {chunkSize} bytes.");

            var result = new List<HircIndexEntry>();
            var hircCount = chunk.ReadUInt32();

            for (uint itemIndex = 0; itemIndex < hircCount; itemIndex++)
            {
                if (chunk.BytesLeft < HircHeader.Size)
                    throw new InvalidDataException($"HIRC item {itemIndex} does not contain a complete header.");

                var itemOffsetInChunk = chunk.Index;
                var header = HircHeader.ReadData(chunk);
                if (header.SectionSize < sizeof(uint))
                    throw new InvalidDataException($"HIRC item {itemIndex} has an invalid section size of {header.SectionSize}.");

                var hircLength = checked(HircHeader.PrefixSize + header.SectionSize);
                if (hircLength > int.MaxValue || hircLength - HircHeader.Size > chunk.BytesLeft)
                    throw new InvalidDataException($"HIRC item {itemIndex} extends beyond its HIRC chunk.");

                result.Add(
                    new HircIndexEntry
                    {
                        Header = header,
                        Offset = payloadOffset + itemOffsetInChunk,
                        Length = (int)hircLength,
                        Index = itemIndex
                    });
                chunk.Advance((int)(hircLength - HircHeader.Size));
            }

            if (chunk.BytesLeft != 0)
                throw new InvalidDataException($"HIRC index left {chunk.BytesLeft} unread bytes in the chunk.");

            return result;
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
