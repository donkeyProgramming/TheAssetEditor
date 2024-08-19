using System;
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
using static Editors.Audio.AudioEditor.AudioEditorData;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.AudioProjectData;
using static Editors.Audio.AudioEditor.SettingsEnumConverter;

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

    public partial class AudioEditorNewAudioProjectViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        readonly ILogger _logger = Logging.Create<AudioEditorNewAudioProjectViewModel>();
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New VO Audio Project");

        // The properties for each settings.
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _customStatesFilePath;
        [ObservableProperty] private string _selectedLanguage;
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;

        // The data the ComboBoxes are populated with.
        [ObservableProperty] private ObservableCollection<Language> _languages = new(Enum.GetValues(typeof(Language)).Cast<Language>());
        [ObservableProperty] private ObservableCollection<DialogueEventType> _audioProjectEventTypes = new(Enum.GetValues(typeof(DialogueEventType)).Cast<DialogueEventType>());
        [ObservableProperty] private ObservableCollection<DialogueEventSubtype> _audioProjectSubtypes = []; // Determined according to what Event Type is selected

        // The Dialogue Event CheckBoxes that are displayed in the Dialogue Events ListBox.
        [ObservableProperty] private ObservableCollection<DialogueEventCheckBox> _dialogueEventCheckBoxes = [];

        // Properties to control whether OK button is enabled.
        [ObservableProperty] private bool _isAudioProjectFileNameSet;
        [ObservableProperty] private bool _isLanguageSelected;
        [ObservableProperty] private bool _isAnyDialogueEventChecked;
        [ObservableProperty] private bool _isOkButtonIsEnabled;

        public AudioEditorNewAudioProjectViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
        }

        partial void OnAudioProjectFileNameChanged(string value)
        {
            IsAudioProjectFileNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            IsLanguageSelected = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();

            // Update the ComboBox for EventSubType upon DialogueEventType selection.
            UpdateAudioProjectEventSubType();
        }

        partial void OnSelectedAudioProjectEventSubtypeChanged(string value)
        {
            DialogueEventCheckBoxes.Clear();

            // Update the ListBox with the appropriate Dialogue Events.
            PopulateDialogueEventsListBox();
        }

        private void HandleDialogueEventCheckBoxChanged(DialogueEventCheckBox changedItem)
        {
            IsAnyDialogueEventChecked = DialogueEventCheckBoxes.Any(checkBox => checkBox.IsChecked);
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonIsEnabled = IsLanguageSelected && IsAudioProjectFileNameSet && IsAnyDialogueEventChecked;
        }

        [RelayCommand] public void SetCustomStatesLocation()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".customstates"]);

            if (browser.ShowDialog())
            {
                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
                CustomStatesFilePath = filePath;
                _logger.Here().Information($"Custom States file path set to: {filePath}");
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
                                Content = AddExtraUnderScoresToString(dialogueEvent.EventName),
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

        [RelayCommand] public void CreateAudioProject()
        {
            if (DialogueEventCheckBoxes.All(checkBox => checkBox.IsChecked != true))
                return;

            // Remove any pre-existing data.
            AudioEditorInstance.ResetAudioEditorData();
            _audioEditorViewModel.ResetAudioEditorViewModelData();

            // Create the list of events to be displayed in the AudioEditor.
            CreateAudioProjectEventsList();

            // Create the object for State Groups with qualifiers so that their keys in the AudioProject dictionary are unique.
            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            // Initialise AudioProject according to the Audio Project settings selected.
            InitialiseVOAudioProject();

            // Add the Audio Project with empty events to the PackFile.
            AddAudioProjectToPackFile(_packFileService);

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
                    _audioEditorViewModel.AudioProjectEvents.Add(RemoveExtraUnderScoresFromString(dialogueEvent));
                }
            }
        }

        public void InitialiseVOAudioProject()
        {
            if (AudioEditorInstance.AudioProject == null)
                AudioEditorInstance.AudioProject = new AudioProject();

            // Create settings.
            var settings = new Settings
            {
                AudioProjectName = AudioProjectFileName,
                Language = LanguageEnumToString[GetLanguageEnumString(SelectedLanguage)],
                CustomStatesFilePath = CustomStatesFilePath
            };

            AudioEditorInstance.AudioProject.Settings = settings;

            // Create Dialogue Events.
            var dialogueEvents = new List<DialogueEvent>();

            foreach (var dialogueEventKey in _audioEditorViewModel.AudioProjectEvents)
            {
                var dialogueEvent = new DialogueEvent
                {
                    DialogueEventName = dialogueEventKey
                };

                dialogueEvents.Add(dialogueEvent);
            }

            AudioEditorInstance.AudioProject.DialogueEvents = dialogueEvents;
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
            var recommendedEvents = FrontendVODialogueEvents.Where(e => e.Categories.Contains("Recommended")).Select(e => e.EventName).ToHashSet();

            // Iterate through the CheckBoxes and set IsChecked for those with matching EventName
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

        public void ResetAudioEditorNewAudioProjectViewModelData()
        {
            AudioProjectFileName = null;
            CustomStatesFilePath = null;
            SelectedAudioProjectEventType = null;
            SelectedAudioProjectEventSubtype = null;
            AudioProjectSubtypes.Clear();
            DialogueEventCheckBoxes.Clear();
            IsAnyDialogueEventChecked = false;
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();

            ResetAudioEditorNewAudioProjectViewModelData();
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
