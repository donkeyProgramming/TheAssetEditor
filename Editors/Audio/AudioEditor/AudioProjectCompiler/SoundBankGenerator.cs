using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Shared.Core.PackFiles;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
{
    public class SoundBankGenerator
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SoundBankGenerator(IPackFileService packFileService, IAudioProjectService audioProjectService, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _applicationSettingsService = applicationSettingsService;
        }

        public static void CompileSoundBanksFromAudioProject(ApplicationSettingsService applicationSettingsService, AudioProjectDataModel audioProject)
        {
            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(applicationSettingsService.CurrentSettings.CurrentGame);
            var actorMixerIds = wwiseIDService.ActorMixerIds;

            //SoundBanks.GetSoundBankEnum();
        }
    }
}
