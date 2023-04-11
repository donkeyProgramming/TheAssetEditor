using Audio.FileFormats;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Audio.FileFormats.WWise.Hirc
{
    public class HircParser : IParser
    {
        public void Parse(string fileName, ByteChunk chunk, SoundDataBase bnkFile)
        {
            bnkFile.HircChuck = new HircChunk();
            bnkFile.HircChuck.ChunkHeader = BnkChunkHeader.CreateFromBytes(chunk);
            bnkFile.HircChuck.NumHircItems = chunk.ReadUInt32();

            var failedItems = new List<uint>();
            var factory = HircFactory.CreateFactory(bnkFile.Header.dwBankGeneratorVersion);

            for (uint itemIndex = 0; itemIndex < bnkFile.HircChuck.NumHircItems; itemIndex++)
            {
                var hircType = (HircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.OwnerFile = fileName;
                    hircItem.Parse(chunk);
                    bnkFile.HircChuck.Hircs.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unkInstance = new CAkUnknown() { ErrorMsg = e.Message, IndexInFile = itemIndex, OwnerFile = fileName };
                    unkInstance.Parse(chunk);
                    bnkFile.HircChuck.Hircs.Add(unkInstance);
                }
            }
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
            Parse("name", new ByteChunk(byteArray), new SoundDataBase() { Header = new Bkhd.BkhdHeader() { dwBankGeneratorVersion = 136 } });

            return byteArray;
        }
    }
}
