using System;
using Audio.FileFormats.WWise.Bkhd;
using Audio.Utility;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class BnkHeaderBuilder
    {
        public BkhdHeader Generate(CompilerData projectFile)
        {
            // throw new System.Exception();
            var bnkName = projectFile.ProjectSettings.BnkName;
            var soundBankId = WWiseHash.Compute(bnkName);
            var header = new BkhdHeader()
            {
                dwBankGeneratorVersion = 0x80000088,
                dwSoundBankID = soundBankId,
                dwLanguageID = 550298558, // English(UK)
                bFeedbackInBank = 0x10,
                dwProjectID = 2361,
                padding = BitConverter.GetBytes(0x04)
            };

            return header;
        }
    }
}
