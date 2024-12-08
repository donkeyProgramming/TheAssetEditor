﻿using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProject;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.GameSettings.Warhammer3.Languages;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class NewAudioProjectViewModel : ObservableObject, IEditorInterface
    {
        readonly ILogger _logger = Logging.Create<NewAudioProjectViewModel>();
        private readonly IPackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        private Action _closeAction;

        public string DisplayName { get; set; } = "New Audio Project";

        // Settings properties.
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _audioProjectDirectory;
        [ObservableProperty] private GameLanguage _selectedLanguage;
        [ObservableProperty] private ObservableCollection<GameLanguage> _languages = new(Enum.GetValues<GameLanguage>());

        // Ok button enablement.
        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isAudioProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isOKButtonEnabled;

        public NewAudioProjectViewModel(IPackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            // Default values.
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
            IsOKButtonEnabled = IsAudioProjectFileNameSet && IsAudioProjectDirectorySet && IsLanguageSelected;
        }

        [RelayCommand] public void SetNewFileLocation()
        {
            var result = _packFileUiProvider.DisplayBrowseFoldersDialog();
            if (result.Result)
            {
                var filePath = result.FolderName;
                AudioProjectDirectory = filePath;
                _logger.Here().Information($"Audio Project directory set to: {filePath}");
            }
        }

        [RelayCommand] public void CreateAudioProject()
        {
            // Reset and initialise data.
            _audioEditorViewModel.ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
            _audioEditorViewModel.InitialiseCollections();

            // Initialise AudioProject according to the Audio Project settings selected.
            _audioProjectService.InitialiseAudioProject(_audioEditorViewModel, AudioProjectFileName, AudioProjectDirectory, GetGameString(SelectedLanguage));

            // Add the Audio Project to the PackFile.
            _audioProjectService.SaveAudioProject(_packFileService);

            CloseWindowAction();
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
