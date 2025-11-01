using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
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
                    SourceBits = (byte)(audioProjectSound.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx) ? 0x00 : 0x01)
                }
            };
        }
    }
}
