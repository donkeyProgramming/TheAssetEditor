using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.AudioEditorMenu
{
    public partial class AudioEditorMenuViewModel
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public AudioEditorMenuViewModel(
            AudioEditorViewModel audioEditorViewModel,
            IAudioRepository audioRepository,
            IPackFileService packFileService,
            IAudioProjectService audioProjectService,
            IStandardDialogs packFileUiProvider,
            ApplicationSettingsService applicationSettingsService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;
            _applicationSettingsService = applicationSettingsService;
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_audioEditorViewModel, _packFileService, _audioProjectService, _packFileUiProvider);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject(_packFileService);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(_audioEditorViewModel, _packFileService, _audioRepository, _packFileUiProvider);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioProjectService.CompileAudioProject(_applicationSettingsService);
        }
    }
}
