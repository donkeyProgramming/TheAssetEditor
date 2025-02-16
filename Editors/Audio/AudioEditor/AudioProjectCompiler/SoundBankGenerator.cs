using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
{
    public class SoundBankGenerator
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public SoundBankGenerator(IPackFileService packFileService, IAudioRepository audioRepository, IAudioProjectService audioProjectService, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
            _applicationSettingsService = applicationSettingsService;
        }

        public void CompileSoundBanksFromAudioProject(AudioProjectDataModel audioProject)
        {
            _audioProjectService.SaveAudioProject(_packFileService);

            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(_applicationSettingsService.CurrentSettings.CurrentGame);
            var actorMixerIds = wwiseIDService.ActorMixerIds;

            //SoundBanks.GetSoundBankEnum();
        }
    }
}
