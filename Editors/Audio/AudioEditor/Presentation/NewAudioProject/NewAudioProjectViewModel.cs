using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.Presentation.NewAudioProject
{
    public partial class NewAudioProjectViewModel : ObservableObject
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioEditorFileService _audioEditorFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IStandardDialogs _standardDialogs;

        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();
        private System.Action _closeAction;

        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _audioProjectDirectory;
        [ObservableProperty] private Wh3Language _selectedLanguage;
        [ObservableProperty] private ObservableCollection<Wh3Language> _languages = new (Enum.GetValues<Wh3Language>());

        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isAudioProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isOkButtonEnabled;

        public NewAudioProjectViewModel(
            IPackFileService packFileService,
            IAudioEditorStateService audioEditorStateService,
            IAudioEditorFileService audioEditorFileService,
            ApplicationSettingsService applicationSettingsService,
            IStandardDialogs standardDialogs)
        {
            _packFileService = packFileService;
            _audioEditorStateService = audioEditorStateService;
            _audioEditorFileService = audioEditorFileService;
            _applicationSettingsService = applicationSettingsService;
            _standardDialogs = standardDialogs;

            AudioProjectDirectory = "audio\\audio_projects";
            SelectedLanguage = Wh3Language.EnglishUK;
        }

        partial void OnAudioProjectFileNameChanged(string value)
        {
            var audioProjectFileNameWithoutSpaces = value.Replace(" ", "_");
            AudioProjectFileName = audioProjectFileNameWithoutSpaces;
            IsAudioProjectFileNameSet = !string.IsNullOrEmpty(AudioProjectFileName);
            UpdateOkButtonIsEnabled();
        }

        partial void OnAudioProjectDirectoryChanged(string value)
        {
            IsAudioProjectDirectorySet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedLanguageChanged(Wh3Language value)
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

            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
            var audioProjectFileNameWithoutSpaces = AudioProjectFileName.Replace(" ", "_");
            var fileName = $"{audioProjectFileNameWithoutSpaces}.aproj";
            var filePath = $"{AudioProjectDirectory}\\{fileName}";
            var language = Wh3LanguageInformation.GetLanguageAsString(SelectedLanguage);

            var audioProject = AudioProjectFile.Create(currentGame, language, audioProjectFileNameWithoutSpaces);

            _audioEditorStateService.StoreAudioProject(audioProject);
            _audioEditorStateService.StoreAudioProjectFileName(fileName);
            _audioEditorStateService.StoreAudioProjectFilePath(filePath);

            _audioEditorFileService.Save(audioProject, fileName, filePath);
            _audioEditorFileService.Load(audioProject, fileName, filePath);

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) => _closeAction = closeAction;
    }
}
