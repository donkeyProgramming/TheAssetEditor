using System;
using Editors.Audio.Utility;
using Shared.GameFormats.WWise.Bkhd;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class BnkHeaderBuilder
    {
        public static BkhdHeader Generate(CompilerData projectFile)
        {
            var bnkName = projectFile.ProjectSettings.BnkName;
            var soundBankId = WwiseHash.Compute(bnkName);
            var language = WwiseHash.Compute(projectFile.ProjectSettings.Language);

            var header = new BkhdHeader()
            {
                DwBankGeneratorVersion = 0x80000088,
                DwSoundBankId = soundBankId,
                DwLanguageId = language,
                BFeedbackInBank = 0x10,
                DwProjectID = 2361,
                Padding = BitConverter.GetBytes(0x04)
            };

            return header;
        }
    }
}
