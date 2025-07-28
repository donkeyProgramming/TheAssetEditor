using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Models;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using static Editors.Audio.GameSettings.Warhammer3.Languages;

namespace Editors.Audio.AudioEditor.NewAudioProject
{
    public partial class NewAudioProjectViewModel : ObservableObject
    {
        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();

        private readonly IEventHub _eventHub;
        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IStandardDialogs _standardDialogs;

        private System.Action _closeAction;

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

        public NewAudioProjectViewModel(IEventHub eventHub, IPackFileService packFileService, IAudioEditorService audioEditorService, IStandardDialogs standardDialogs)
        {
            _eventHub = eventHub;
            _packFileService = packFileService;
            _audioEditorService = audioEditorService;
            _standardDialogs = standardDialogs;

            AudioProjectDirectory = "audio_projects";
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

            // Reset data
            _audioEditorService.ResetAudioProject();

            // Initialise AudioProject according to the Audio Project settings selected
            _audioEditorService.InitialiseAudioProject(_eventHub, AudioProjectFileName, AudioProjectDirectory, GameLanguageStringLookup[SelectedLanguage]);

            // Add the Audio Project to the PackFile
            var audioProject = AudioProject.GetAudioProject(_audioEditorService.AudioProject);
            _audioEditorService.SaveAudioProject(audioProject, audioProject.FileName, audioProject.DirectoryPath);

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();
        }

        public void SetCloseAction(System.Action closeAction)
        {
            _closeAction = closeAction;
        }
    }
}
