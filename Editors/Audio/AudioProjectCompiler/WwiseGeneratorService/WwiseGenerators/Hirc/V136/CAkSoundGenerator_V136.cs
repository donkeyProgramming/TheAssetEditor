using Editors.Audio.AudioEditor.Models;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class CAkSoundGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem)
        {
            var audioProjectSound = audioProjectItem as Sound;
            var soundHirc = CreateSoundHirc(audioProjectSound);
            soundHirc.AkBankSourceData = AkBankSourceDataGenerator_V136.CreateAkBankSourceData(audioProjectSound);
            soundHirc.NodeBaseParams = NodeBaseParamsGenerator_V136.CreateNodeBaseParams(audioProjectSound);
            soundHirc.UpdateSectionSize();
            return soundHirc;
        }

        private static CAkSound_V136 CreateSoundHirc(Sound audioProjectSound)
        {
            return new CAkSound_V136()
            {
                Id = audioProjectSound.Id,
                HircType = audioProjectSound.HircType,
            };
        }
    }
}
