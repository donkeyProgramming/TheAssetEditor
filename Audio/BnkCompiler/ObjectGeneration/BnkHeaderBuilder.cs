using Audio.FileFormats.WWise.Bkhd;
using Audio.Utility;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System.IO;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class BnkHeaderBuilder
    {
        public BkhdHeader Generate(AudioProjectXml projectFile)
        {
            var bnkName = Path.GetFileNameWithoutExtension(projectFile.OutputFile);
            var soundBankId = WWiseHash.Compute(bnkName);
            var header = new BkhdHeader()
            {
                dwBankGeneratorVersion = 0x80000088,
                dwSoundBankID = soundBankId,
                dwLanguageID = 550298558, // English(UK)
                bFeedbackInBank = 0x10,
                dwProjectID = 2361,
                padding = 0x04,
            };

            return header;
        }
    }
}
