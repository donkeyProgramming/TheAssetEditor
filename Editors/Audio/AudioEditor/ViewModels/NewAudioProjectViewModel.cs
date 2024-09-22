using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class NewAudioProjectViewModel : ObservableObject, IEditorViewModel
    {
        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New Audio Project");

        // The properties for each settings.
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _audioProjectDirectory;
        [ObservableProperty] private Language _selectedLanguage;

        // The data the ComboBoxes are populated with.
        [ObservableProperty] private ObservableCollection<Language> _languages = new(Enum.GetValues(typeof(Language)).Cast<Language>());

        // Properties to control whether OK button is enabled.
        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isAudioProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isOKButtonEnabled;

        public NewAudioProjectViewModel(PackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;

            // Default values.
            AudioProjectDirectory = "AudioProjects";
            SelectedLanguage = Language.EnglishUK;
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

        partial void OnSelectedLanguageChanged(Language value)
        {
            IsLanguageSelected = !string.IsNullOrEmpty(value.ToString());
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOKButtonEnabled = IsAudioProjectFileNameSet && IsAudioProjectDirectorySet && IsLanguageSelected;
        }

        [RelayCommand] public void SetNewFileLocation()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".OleIsADonkey"], true); // Set it to some non-existant file type and it will show only folders.

            if (browser.ShowDialog())
            {
                var filePath = browser.SelectedPath;
                AudioProjectDirectory = filePath;
                _logger.Here().Information($"Audio Project directory set to: {filePath}");
            }
        }

        [RelayCommand] public void CreateAudioProject()
        {
            // Reset and initialise data.
            _audioEditorViewModel.ResetAudioProjectConfiguration();
            _audioEditorViewModel.ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
            _audioEditorViewModel.InitialiseCollections();

            // Initialise AudioProject according to the Audio Project settings selected.
            _audioProjectService.InitialiseAudioProject(AudioProjectFileName, AudioProjectDirectory, GetStringFromLanguage(SelectedLanguage));

            // Add the Audio Project to the PackFile.
            _audioProjectService.SaveAudioProject(_packFileService);

            CloseWindowAction();

            // Set visibility of UI elements.
            _audioEditorViewModel.SetAudioEditorVisibility(true);
        }

        public void ResetNewAudioProjectViewModelData()
        {
            AudioProjectFileName = null;
            AudioProjectDirectory = null;
            IsAudioProjectFileNameSet = false;
            IsAudioProjectDirectorySet = false;
            IsLanguageSelected = false;
            IsOKButtonEnabled = false;
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

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
