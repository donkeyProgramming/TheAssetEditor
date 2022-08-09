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
                Size = chunk.ReadUInt32(),

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
    }

    public class BkhdHeader
    {

        public string OwnerFileName { get; set; }

        public uint Size { get; set; }  // Not acutally part of header
        public uint dwBankGeneratorVersion { get; set; }
        public uint dwSoundBankID { get; set; }     // Name of the file
        public uint dwLanguageID { get; set; }      // Enum 11 - English
        public uint bFeedbackInBank { get; set; }
        public uint dwProjectID { get; set; }
        public uint padding { get; set; }


        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue(Size, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(dwBankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(dwSoundBankID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(dwLanguageID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(bFeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(dwProjectID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(padding, out _));
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var parser = new BkhdParser();
            parser.Parse("name", new ByteChunk(byteArray), new SoundDataBase());

            return byteArray;
        }
    }

}
