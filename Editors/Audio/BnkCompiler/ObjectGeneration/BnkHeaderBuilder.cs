using System;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Bkhd;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class BnkHeaderBuilder
    {
        public static BkhdChunk Generate(CompilerData projectFile)
        {
            var bnkName = projectFile.ProjectSettings.BnkName;
            var soundBankId = WwiseHash.Compute(bnkName);
            var language = WwiseHash.Compute(projectFile.ProjectSettings.Language);

            var akBankHeader = new AkBankHeader()
            {
                BankGeneratorVersion = 0x80000088,
                SoundBankID = soundBankId,
                LanguageID = language,
                FeedbackInBank = 0x10,
                ProjectID = 2361,
                Padding = BitConverter.GetBytes(0x04)
            };

            var bkhdChunk = new BkhdChunk
            {
                AkBankHeader = akBankHeader
            };

            return bkhdChunk;
        }
    }
}
