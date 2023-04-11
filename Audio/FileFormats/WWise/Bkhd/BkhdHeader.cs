using Audio.FileFormats;
using Filetypes.ByteParsing;
using System.IO;

namespace Audio.FileFormats.WWise.Bkhd
{
    public class BkhdHeader
    {
        public string OwnerFileName { get; set; }

        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = "BKHD", ChunkSize = 0x18 };

        public uint dwBankGeneratorVersion { get; set; }
        public uint dwSoundBankID { get; set; }     // Name of the file
        public uint dwLanguageID { get; set; }      // Enum 11 - English
        public uint bFeedbackInBank { get; set; }
        public uint dwProjectID { get; set; }
        public uint padding { get; set; }
    }

}
