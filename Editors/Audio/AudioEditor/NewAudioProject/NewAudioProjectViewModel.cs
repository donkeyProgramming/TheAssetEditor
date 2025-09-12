using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.NewAudioProject
{
    public partial class NewAudioProjectViewModel : ObservableObject
    {
        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();

        private readonly IEventHub _eventHub;
        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioProjectFileService _audioProjectFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IStandardDialogs _standardDialogs;

        private System.Action _closeAction;

        // Settings properties
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _audioProjectDirectory;
        [ObservableProperty] private Wh3GameLanguage _selectedLanguage;
        [ObservableProperty] private ObservableCollection<Wh3GameLanguage> _languages = new(Enum.GetValues<Wh3GameLanguage>());

        // Ok button enablement
        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isAudioProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isOkButtonEnabled;

        public NewAudioProjectViewModel(
            IEventHub eventHub,
            IPackFileService packFileService,
            IAudioEditorStateService audioEditorStateService,
            IAudioProjectFileService audioProjectFileService,
            ApplicationSettingsService applicationSettingsService,
            IStandardDialogs standardDialogs)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;
            _audioEditorStateService = audioEditorStateService;
            _audioProjectFileService = audioProjectFileService;
            _applicationSettingsService = applicationSettingsService;
            _standardDialogs = standardDialogs;

            AudioProjectDirectory = "audio\\audio_projects";
            SelectedLanguage = Wh3GameLanguage.EnglishUK;
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

        partial void OnSelectedLanguageChanged(Wh3GameLanguage value)
        {
            IsLanguageSelected = !string.IsNullOrEmpty(value.ToString());
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonEnabled = IsAudioProjectFileNameSet && IsAudioProjectDirectorySet && IsLanguageSelected;
        }

        [RelayCommand]
        public void SetNewFileLocation()
        {
            var result = _standardDialogs.DisplayBrowseFolderDialog();
            if (result.Result)
            {
                var filePath = result.Folder;
                AudioProjectDirectory = filePath;
                _logger.Here().Information($"Audio Project directory set to: {filePath}");
            }
        }

        [RelayCommand]
        public void CreateAudioProject()
        {
            if (_packFileService.GetEditablePack() == null)
            {
                CloseWindowAction();
                return;
            }

            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
            var fileName = $"{AudioProjectFileName}.aproj";
            var filePath = $"{AudioProjectDirectory}\\{fileName}";
            var language = Wh3LanguageInformation.GetGameLanguageAsString(SelectedLanguage);

            var audioProject = AudioProject.Create(currentGame, language);
            _audioProjectFileService.Save(audioProject, fileName, filePath);

            _audioEditorStateService.AudioProject = audioProject;
            _audioEditorStateService.AudioProjectFileName = fileName;
            _audioEditorStateService.AudioProjectFilePath = filePath;

            _eventHub.Publish(new AudioProjectInitialisedEvent());

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) => _closeAction = closeAction;
    }
}
