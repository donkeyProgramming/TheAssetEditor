﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.AudioProject;
using static Editors.Audio.AudioEditor.SettingsEnumConverter;
using static Editors.Audio.AudioEditor.VOProjectData;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public class DialogueEventCheckBox : ObservableObject
    {
        private bool _isChecked;

        public string Content { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }
    }

    public partial class NewVOAudioProjectViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        readonly ILogger _logger = Logging.Create<NewVOAudioProjectViewModel>();
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New VO Project");

        // The properties for each settings.
        [ObservableProperty] private string _vOProjectFileName;
        [ObservableProperty] private string _vOProjectDirectory;
        [ObservableProperty] private string _statesProjectFilePath;
        [ObservableProperty] private Language _selectedLanguage;
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;

        // The data the ComboBoxes are populated with.
        [ObservableProperty] private ObservableCollection<Language> _languages = new(Enum.GetValues(typeof(Language)).Cast<Language>());
        [ObservableProperty] private ObservableCollection<DialogueEventType> _audioProjectEventTypes = new(Enum.GetValues(typeof(DialogueEventType)).Cast<DialogueEventType>());
        [ObservableProperty] private ObservableCollection<DialogueEventSubtype> _audioProjectSubtypes = []; // Determined according to what Event Type is selected

        // The Dialogue Event CheckBoxes that are displayed in the Dialogue Events ListBox.
        [ObservableProperty] private ObservableCollection<DialogueEventCheckBox> _dialogueEventCheckBoxes = [];

        // Properties to control whether OK button is enabled.
        [ObservableProperty] private bool _isVOProjectFileNameSet;
        [ObservableProperty] private bool _isVOProjectDirectorySet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isAnyDialogueEventChecked;
        [ObservableProperty] private bool _isOkButtonIsEnabled;

        public NewVOAudioProjectViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;

            // Default value set here
            VOProjectDirectory = "audioprojects";  
            SelectedLanguage = Language.EnglishUK;
        }

        partial void OnVOProjectFileNameChanged(string value)
        {
            IsVOProjectFileNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnVOProjectDirectoryChanged(string value)
        {
            IsVOProjectDirectorySet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedLanguageChanged(Language value)
        {
            IsLanguageSelected = !string.IsNullOrEmpty(value.ToString());
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();
            IsAnyDialogueEventChecked = false;

            // Update the ComboBox for EventSubType upon DialogueEventType selection.
            UpdateAudioProjectEventSubType();
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedAudioProjectEventSubtypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();
            IsAnyDialogueEventChecked = false;

            // Update the ListBox with the appropriate Dialogue Events.
            PopulateDialogueEventsListBox();
            UpdateOkButtonIsEnabled();
        }

        private void HandleDialogueEventCheckBoxChanged(DialogueEventCheckBox changedItem)
        {
            IsAnyDialogueEventChecked = DialogueEventCheckBoxes.Any(checkBox => checkBox.IsChecked);
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonIsEnabled = IsVOProjectFileNameSet && IsVOProjectDirectorySet && IsLanguageSelected && IsAnyDialogueEventChecked;
        }

        [RelayCommand] public void SetNewFileLocation()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".OleIsADonkey"], true); // Set it to some non-existant file type and it will show only folders.

            if (browser.ShowDialog())
            {
                var filePath = browser.SelectedPath;
                VOProjectDirectory = filePath;
                _logger.Here().Information($"Custom States file path set to: {filePath}");
            }
        }

        [RelayCommand] public void SetCustomStatesLocation()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".customstates"]);

            if (browser.ShowDialog())
            {
                var filePath = browser.SelectedPath;
                StatesProjectFilePath = filePath;
                _logger.Here().Information($"Custom States file path set to: {filePath}");
            }
        }

        [RelayCommand] public void SelectAll()
        {
            foreach (var checkBox in DialogueEventCheckBoxes)
                checkBox.IsChecked = true;
        }

        /*
        [RelayCommand] public void SelectRecommended()
        {
            // Get the list of dialogue events with the "Recommended" category
            var recommendedEvents = FrontendVODialogueEvents.Where(e => e.Categories.Contains("Recommended")).Select(e => e.Name).ToHashSet();

            // Iterate through the CheckBoxes and set IsChecked for those with matching Name
            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (recommendedEvents.Contains(checkBox.Content.ToString()))
                {
                    checkBox.IsChecked = true;
                }
            }
        }
        */

        [RelayCommand] public void SelectNone()
        {
            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
        }

        public void UpdateAudioProjectEventSubType()
        {
            AudioProjectSubtypes.Clear();

            if (SelectedAudioProjectEventType != null && Enum.TryParse(SelectedAudioProjectEventType.ToString(), out DialogueEventType eventType))
            {
                if (DialogueEventTypeToSubtypes.TryGetValue(eventType, out var subtypes))
                {
                    foreach (var subtype in subtypes)
                        AudioProjectSubtypes.Add(subtype);
                }
            }
        }

        public void PopulateDialogueEventsListBox()
        {
            if (Enum.TryParse(SelectedAudioProjectEventType, out DialogueEventType eventType))
            {
                if (Enum.TryParse(SelectedAudioProjectEventSubtype, out DialogueEventSubtype eventSubtype))
                {
                    var dialogueEvents = DialogueEvents
                        .Where(dialogueEvent => dialogueEvent.Type == eventType)
                        .ToList();

                    foreach (var dialogueEvent in dialogueEvents)
                    {
                        if (dialogueEvent.Subtype.Contains(eventSubtype))
                        {
                            var item = new DialogueEventCheckBox
                            {
                                Content = AddExtraUnderscoresToString(dialogueEvent.EventName),
                                IsChecked = false
                            };

                            // Subscribe to property changes
                            item.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(DialogueEventCheckBox.IsChecked))
                                {
                                    HandleDialogueEventCheckBoxChanged(item);
                                }
                            };

                            DialogueEventCheckBoxes.Add(item);
                        }
                    }
                }
            }
        }

        [RelayCommand] public void CreateVOProject()
        {
            // Remove any pre-existing data.
            AudioProjectInstance.ResetAudioProjectData();
            _audioEditorViewModel.ResetAudioEditorViewModelData();

            // Create the list of events to be displayed in the AudioEditor.
            CreateAudioProjectEventsList();

            // Create the object for State Groups with qualifiers so that their keys in the AudioProject dictionary are unique.
            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            // Initialise AudioProject according to the Audio Project settings selected.
            InitialiseVOAudioProject();

            // Add the VO Project with empty events to the PackFile.
            AddToPackFile(_packFileService, AudioProjectInstance.VOProject, AudioProjectInstance.FileName, AudioProjectInstance.Directory, AudioProjectInstance.Type);

            // Load the custom States so that they can be referenced when the Event is loaded.
            //PrepareCustomStatesForComboBox(this);

            CloseWindowAction();
        }

        public void CreateAudioProjectEventsList()
        {
            _audioEditorViewModel.AudioProjectEvents.Clear();

            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (checkBox.IsChecked == true)
                {
                    var dialogueEvent = checkBox.Content.ToString();
                    _audioEditorViewModel.AudioProjectEvents.Add(RemoveExtraUnderscoresFromString(dialogueEvent));
                }
            }
        }

        public void InitialiseVOAudioProject()
        {
            if (AudioProjectInstance.VOProject == null)
                AudioProjectInstance.VOProject = new VOProject();

            AudioProjectInstance.Type = ProjectType.voproject;
            AudioProjectInstance.FileName = VOProjectFileName;
            AudioProjectInstance.Directory = VOProjectDirectory;

            // Create settings.
            var settings = new Settings
            {
                Language = LanguageEnumToString[GetLanguageEnumString(SelectedLanguage.ToString())],
                StatesProjectFilePath = StatesProjectFilePath
            };

            AudioProjectInstance.VOProject.Settings = settings;

            // Create Dialogue Events.
            var dialogueEvents = new List<DialogueEvent>();

            foreach (var dialogueEventKey in _audioEditorViewModel.AudioProjectEvents)
            {
                var dialogueEvent = new DialogueEvent
                {
                    Name = dialogueEventKey
                };

                dialogueEvents.Add(dialogueEvent);
            }

            AudioProjectInstance.VOProject.DialogueEvents = dialogueEvents;
        }

        public void ResetNewVOAudioProjectViewModelData()
        {
            VOProjectFileName = null;
            VOProjectDirectory = null;
            StatesProjectFilePath = null;
            SelectedAudioProjectEventType = null;
            SelectedAudioProjectEventSubtype = null;
            AudioProjectSubtypes.Clear();
            DialogueEventCheckBoxes.Clear();
            IsVOProjectFileNameSet = false;
            IsVOProjectDirectorySet = false;
            IsLanguageSelected = false;
            IsAnyDialogueEventChecked = false;
            IsOkButtonIsEnabled = false;
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();
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