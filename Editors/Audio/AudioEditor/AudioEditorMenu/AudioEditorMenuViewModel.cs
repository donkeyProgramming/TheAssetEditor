using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioEditor.NewAudioProject;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.AudioEditorMenu
{
    public partial class AudioEditorMenuViewModel
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public AudioEditorMenuViewModel(
            IPackFileService packFileService,
            IAudioProjectService audioProjectService,
            IStandardDialogs packFileUiProvider)
        {
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(AudioEditorViewModel, _packFileService, _audioProjectService, _packFileUiProvider);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(AudioEditorViewModel);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioProjectService.CompileAudioProject();
        }
    }
}
