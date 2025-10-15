using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    public class CAkSoundGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
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
