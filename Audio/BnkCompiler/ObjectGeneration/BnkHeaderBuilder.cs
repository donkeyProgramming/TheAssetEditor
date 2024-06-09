using System;
using Audio.Utility;
using Shared.GameFormats.WWise.Bkhd;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class BnkHeaderBuilder
    {
        public static BkhdHeader Generate(CompilerData projectFile)
        {
            var bnkName = projectFile.ProjectSettings.BnkName;
            var soundBankId = WWiseHash.Compute(bnkName);
            var language = WWiseHash.Compute(projectFile.ProjectSettings.Language);

            var header = new BkhdHeader()
            {
                dwBankGeneratorVersion = 0x80000088,
                dwSoundBankId = soundBankId,
                dwLanguageId = language,
                bFeedbackInBank = 0x10,
                dwProjectID = 2361,
                padding = BitConverter.GetBytes(0x04)
            };

            return header;
        }
    }
}
