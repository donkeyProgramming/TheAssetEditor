using System;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.Wwise.Generators.Bkhd
{
    class BkhdChunkGenerator
    {
        public static BkhdChunk GenerateBkhdChunk(uint bankGeneratorVersion, uint soundBankId, uint language, uint wwiseProjectId)
        {
            return new BkhdChunk
            {
                ChunkHeader = ChunkHeaderGenerator.GenerateChunkHeader(BankChunkTypes.BKHD, 0x18),
                AkBankHeader = new AkBankHeader()
                {
                    BankGeneratorVersion = bankGeneratorVersion,
                    SoundBankId = soundBankId,
                    LanguageId = language,
                    AltValues = 0x10,
                    ProjectId = wwiseProjectId,
                    Padding = BitConverter.GetBytes(0x04)
                }
            };
        }
    }
}
