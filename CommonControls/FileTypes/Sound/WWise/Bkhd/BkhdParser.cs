using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Bkhd
{
    public class BkhdParser : IParser
    {
        public void Parse(string fileName, ByteChunk chunk, SoundDataBase soundDb)
        {
            var bkdh = new BkhdHeader()
            {
                OwnerFileName = fileName,
                ChunkHeader = BnkChunkHeader.CreateFromBytes(chunk),

                dwBankGeneratorVersion = chunk.ReadUInt32(),
                dwSoundBankID = chunk.ReadUInt32(),
                dwLanguageID = chunk.ReadUInt32(),
                bFeedbackInBank = chunk.ReadUInt32(),
                dwProjectID = chunk.ReadUInt32(),
                padding = chunk.ReadUInt32(),
            };

            if (bkdh.dwBankGeneratorVersion == 2147483770)
                bkdh.dwBankGeneratorVersion = 122;

            if (bkdh.dwBankGeneratorVersion == 2147483784)
                bkdh.dwBankGeneratorVersion = 136;

            soundDb.Header = bkdh;
        }

        public static byte[] GetAsByteArray(BkhdHeader header)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(header.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwBankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwSoundBankID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwLanguageID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.bFeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.dwProjectID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.padding, out _));
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            parser.Parse("name", new ByteChunk(byteArray), new SoundDataBase());

            return byteArray;
        }
    }

}
