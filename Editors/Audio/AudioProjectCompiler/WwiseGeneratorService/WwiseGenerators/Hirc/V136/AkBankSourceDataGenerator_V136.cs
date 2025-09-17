using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class AkBankSourceDataGenerator_V136
    {
        public static AkBankSourceData_V136 CreateAkBankSourceData(Sound audioProjectSound)
        {
            return new AkBankSourceData_V136()
            {
                PluginId = 0x00040001,
                StreamType = AKBKSourceType.Streaming,
                AkMediaInformation = new AkBankSourceData_V136.AkMediaInformation_V136()
                {
                    SourceId = audioProjectSound.SourceId,
                    InMemoryMediaSize = (uint)audioProjectSound.InMemoryMediaSize,
                    SourceBits = (byte)(audioProjectSound.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx) ? 0x00 : 0x01)
                }
            };
        }
    }
}
