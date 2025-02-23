using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
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
        private readonly IFileSaveService _fileSaveService;
        private readonly SoundPlayer _soundPlayer;
        private readonly WemGenerator _wemGenerator;

        public AudioEditorMenuViewModel(
            AudioEditorViewModel audioEditorViewModel,
            IAudioRepository audioRepository,
            IPackFileService packFileService,
            IAudioProjectService audioProjectService,
            IStandardDialogs packFileUiProvider,
            ApplicationSettingsService applicationSettingsService,
            IFileSaveService fileSaveService,
            SoundPlayer soundPlayer,
            WemGenerator wemGenerator)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;
            _applicationSettingsService = applicationSettingsService;
            _fileSaveService = fileSaveService;
            _soundPlayer = soundPlayer;
            _wemGenerator = wemGenerator;
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
            _audioProjectService.CompileAudioProject(_packFileService, _audioRepository, _applicationSettingsService, _fileSaveService, _soundPlayer, _wemGenerator);
        }
    }
}
