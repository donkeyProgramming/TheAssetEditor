using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.GameSettings.Warhammer3.Languages;

namespace Editors.Audio.AudioEditor.NewAudioProject
{
    public partial class NewAudioProjectViewModel : ObservableObject, IEditorInterface
    {
        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();

        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IStandardDialogs _standardDialogs;

        private Action _closeAction;

        public string DisplayName { get; set; } = "New Audio Project";

        // Settings properties
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _audioProjectDirectory;
        [ObservableProperty] private GameLanguage _selectedLanguage;
        [ObservableProperty] private ObservableCollection<GameLanguage> _languages = new(Enum.GetValues<GameLanguage>());

        // Ok button enablement
        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isAudioProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isOkButtonEnabled;

        public NewAudioProjectViewModel(IPackFileService packFileService, IAudioEditorService audioEditorService, IStandardDialogs standardDialogs)
        {
            _packFileService = packFileService;
            _audioEditorService = audioEditorService;
            _standardDialogs = standardDialogs;

            AudioProjectDirectory = "AudioProjects";
            SelectedLanguage = GameLanguage.EnglishUK;
        }

        partial void OnAudioProjectFileNameChanged(string value)
        {
            IsAudioProjectFileNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnAudioProjectDirectoryChanged(string value)
        {
            IsAudioProjectDirectorySet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedLanguageChanged(GameLanguage value)
        {
            IsLanguageSelected = !string.IsNullOrEmpty(value.ToString());
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonEnabled = IsAudioProjectFileNameSet && IsAudioProjectDirectorySet && IsLanguageSelected;
        }

        [RelayCommand] public void SetNewFileLocation()
        {
            var result = _standardDialogs.DisplayBrowseFolderDialog();
            if (result.Result)
            {
                var filePath = result.Folder;
                AudioProjectDirectory = filePath;
                _logger.Here().Information($"Audio Project directory set to: {filePath}");
            }
        }

        [RelayCommand] public void CreateAudioProject()
        {
            if (_packFileService.GetEditablePack() == null)
            {
                CloseWindowAction();
                return;
            }

            // Reset and initialise data
            _audioEditorService.AudioEditorViewModel.ResetAudioEditorViewModelData();
            _audioEditorService.ResetAudioProject();
            _audioEditorService.AudioEditorViewModel.Initialise();

            // Initialise AudioProject according to the Audio Project settings selected
            _audioEditorService.InitialiseAudioProject(AudioProjectFileName, AudioProjectDirectory, GameLanguageStringLookup[SelectedLanguage]);

            // Add the Audio Project to the PackFile
            _audioEditorService.SaveAudioProject();

            CloseWindowAction();
        }

        public void ResetNewAudioProjectViewModelData()
        {
            AudioProjectFileName = null;
            AudioProjectDirectory = null;
            IsAudioProjectFileNameSet = false;
            IsAudioProjectDirectorySet = false;
            IsLanguageSelected = false;
            IsOkButtonEnabled = false;
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();
            ResetNewAudioProjectViewModelData();
        }

        public void SetCloseAction(Action closeAction)
        {
            _closeAction = closeAction;
        }

        public void Close() { }
    }
}
