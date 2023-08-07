using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Audio.FileFormats.WWise.Hirc
{
    public class HircParser
    {
        public bool UseByteFactory { get; set; } = false;

        public HircParser()
        {
        }

        HircFactory GetHircFactory(uint bnkVersion)
        {
            if (UseByteFactory)
                return HircFactory.CreateByteHircFactory();

            return HircFactory.CreateFactory(bnkVersion);
        }

        public HircChunk Parse(string fileName, ByteChunk chunk, uint bnkVersion)
        {
            var hircChuck = new HircChunk
            {
                ChunkHeader = BnkChunkHeader.CreateFromBytes(chunk),
                NumHircItems = chunk.ReadUInt32()
            };

            var failedItems = new List<uint>();
            HircFactory factory = GetHircFactory(bnkVersion);

            for (uint itemIndex = 0; itemIndex < hircChuck.NumHircItems; itemIndex++)
            {
                var hircType = (HircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.ByteIndexInFile = itemIndex;
                    hircItem.OwnerFile = fileName;
                    hircItem.Parse(chunk);
                    hircChuck.Hircs.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unkInstance = new CAkUnknown() { ErrorMsg = e.Message, ByteIndexInFile = itemIndex, OwnerFile = fileName };
                    unkInstance.Parse(chunk);
                    hircChuck.Hircs.Add(unkInstance);
                }
            }

            return hircChuck;
        }

        public byte[] GetAsBytes(HircChunk hircChunk)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(hircChunk.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(hircChunk.NumHircItems, out _));

            foreach (var hircItem in hircChunk.Hircs)
            {
                var bytes = hircItem.GetAsByteArray();
                memStream.Write(bytes);
            }

            var byteArray = memStream.ToArray();

            // For sanity, read back
            Parse("name", new ByteChunk(byteArray), 136);

            return byteArray;
        }
    }
}
