using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.AudioEditor.Data;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
{
    public class SoundBankGenerator
    {
        public static void CompileSoundBanksFromAudioProject(AudioProjectDataModel audioProject, ApplicationSettingsService applicationSettingsService)
        {
            //var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);

            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(applicationSettingsService.CurrentSettings.CurrentGame);
            var test = wwiseIDService.ActorMixerIds;


            //ActorMixerIds
        }
    }
}
