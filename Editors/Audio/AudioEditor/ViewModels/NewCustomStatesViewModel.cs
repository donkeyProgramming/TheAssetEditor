using System;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject;
using static Editors.Audio.AudioEditor.StatesProjectData;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class NewCustomStatesViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        readonly ILogger _logger = Logging.Create<NewCustomStatesViewModel>();
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New Custom States");

        // The properties for each settings.
        [ObservableProperty] private string _statesProjectFileName; 
        [ObservableProperty] private string _statesProjectDirectory;

        // Properties to control whether OK button is enabled.
        [ObservableProperty] private bool _isStatesProjectFileNameSet;
        [ObservableProperty] private bool _isStatesProjectDirectorySet;
        [ObservableProperty] private bool _isOkButtonIsEnabled;

        public NewCustomStatesViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;

            StatesProjectDirectory = "audioprojects";
        }

        partial void OnStatesProjectFileNameChanged(string value)
        {
            IsStatesProjectFileNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnStatesProjectDirectoryChanged(string value)
        {
            IsStatesProjectDirectorySet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonIsEnabled = IsStatesProjectFileNameSet && IsStatesProjectDirectorySet;
        }

        [RelayCommand] public void SetNewFileLocation()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".OleIsADonkey"], true); //Set it to some non-existant file type and it will show only folders.

            if (browser.ShowDialog())
            {
                var filePath = browser.SelectedPath;
                StatesProjectDirectory = filePath;
                _logger.Here().Information($"Custom States file path set to: {filePath}");
            }
        }

        [RelayCommand] public void CreateStatesProject()
        {
            // Remove any pre-existing data.
            AudioProjectInstance.ResetAudioProjectData();
            _audioEditorViewModel.ResetAudioEditorViewModelData();

            var dialogueEvent = "modded_states";
            _audioEditorViewModel.AudioProjectEvents.Add(dialogueEvent);

            // Initialise States Project.
            InitialiseStatesProject();

            // Add the States Project with empty events to the PackFile.
            AddToPackFile(_packFileService, AudioProjectInstance.StatesProject, AudioProjectInstance.FileName, AudioProjectInstance.Directory, AudioProjectInstance.Type);

            CloseWindowAction();
        }

        public void InitialiseStatesProject()
        {
            if (AudioProjectInstance.StatesProject == null)
                AudioProjectInstance.StatesProject = new StatesProject();

            AudioProjectInstance.Type = ProjectType.statesproject;
            AudioProjectInstance.FileName = StatesProjectFileName;
            AudioProjectInstance.Directory = StatesProjectDirectory;
        }

        public void ResetNewCustomStatesViewModelData()
        {
            StatesProjectFileName = null;
            StatesProjectDirectory = null;
            IsStatesProjectFileNameSet = false;
            IsStatesProjectDirectorySet = false;
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();

            ResetNewCustomStatesViewModelData();
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
