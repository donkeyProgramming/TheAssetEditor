using System;
using Editors.Audio.AudioEditor;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Bkhd;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Bkhd
{
    class BkhdChunkGenerator
    {
        public static BkhdChunk GenerateBkhdChunk(AudioProject audioProject, uint bankGeneratorVersion, SoundBank soundBank)
        {
            var bkhdChunk = new BkhdChunk();
            var akBankHeader = new AkBankHeader()
            {
                BankGeneratorVersion = bankGeneratorVersion,
                SoundBankId = soundBank.Id,
                LanguageId = WwiseHash.Compute(audioProject.Language),
                FeedbackInBank = 0x10,
                ProjectId = 2361, // TODO: Need a way to get the project Id via a factory and service.
                Padding = BitConverter.GetBytes(0x04)
            };
            bkhdChunk.AkBankHeader = akBankHeader;
            return bkhdChunk;
        }
    }
}
