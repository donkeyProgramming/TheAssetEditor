using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Bkhd
{
    public class BkhdParser : IParser
    {
        public void Parse(ByteChunk chunk, SoundDataBase soundDb)
        {
            var bkdh = new BkhdHeader()
            {
                Size = chunk.ReadUInt32(),
                dwBankGeneratorVersion = chunk.ReadUInt32(),
                dwSoundBankID = chunk.ReadUInt32(),
                dwLanguageID = chunk.ReadUInt32(),
                bFeedbackInBank = chunk.ReadUInt32(),
                dwProjectID = chunk.ReadUInt32(),
                padding = chunk.ReadUInt32(),
            };
            soundDb.Header = bkdh;
        }
    }

    public class BkhdHeader
    {
        public uint Size { get; set; }
        public uint dwBankGeneratorVersion { get; set; }
        public uint dwSoundBankID { get; set; }     // Name of the file
        public uint dwLanguageID { get; set; }      // Enum 11 - English
        public uint bFeedbackInBank { get; set; }
        public uint dwProjectID { get; set; }
        public uint padding { get; set; }
    }

}
