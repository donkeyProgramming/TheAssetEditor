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

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New Audio Editor Project");

        // The properties for each settings.
        [ObservableProperty] private string _audioProjectFileName;
        [ObservableProperty] private string _customStatesFilePath;
        [ObservableProperty] private string _selectedLanguage;
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;

        // The data the ComboBoxes are populated with.
        [ObservableProperty] private ObservableCollection<AudioEditorSettings.Language> _languages = new(Enum.GetValues(typeof(AudioEditorSettings.Language)).Cast<AudioEditorSettings.Language>());
        [ObservableProperty] private ObservableCollection<AudioEditorSettings.EventType> _audioProjectEventTypes = new(Enum.GetValues(typeof(AudioEditorSettings.EventType)).Cast<AudioEditorSettings.EventType>());
        [ObservableProperty] private ObservableCollection<AudioEditorSettings.EventSubtype> _audioProjectSubtypes = []; // Determined according to what Event Type is selected

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

            // Update the ComboBox for EventSubType upon EventType selection.
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
            using var browser = new PackFileBrowserWindow(_packFileService, [".json"]);

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

            if (SelectedAudioProjectEventType != null && Enum.TryParse(SelectedAudioProjectEventType.ToString(), out AudioEditorSettings.EventType eventType))
            {
                if (AudioEditorSettings.EventTypeToSubtypes.TryGetValue(eventType, out var subtypes))
                {
                    foreach (var subtype in subtypes)
                        AudioProjectSubtypes.Add(subtype);
                }
            }
        }

        public void PopulateDialogueEventsListBox()
        {
            if (Enum.TryParse(SelectedAudioProjectEventType, out AudioEditorSettings.EventType eventType))
            {
                if (Enum.TryParse(SelectedAudioProjectEventSubtype, out AudioEditorSettings.EventSubtype eventSubtype))
                {
                    var dialogueEvents = AudioEditorSettings.DialogueEvents
                        .Where(de => de.Type == eventType)
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

            // Set the AudioEditorInstance data.
            //AudioEditorInstance.AudioProjectFileName = AudioProjectFileName;
            //AudioEditorInstance.CustomStatesFilePath = CustomStatesFilePath;

            // Create the list of events to be displayed in the AudioEditor.
            CreateAudioProjectDialogueEventsList();

            // Create the object for State Groups with qualifiers so that their keys in the AudioProjectConverter dictionary are unique.
            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            // Initialise AudioProjectConverter according to the Audio Project settings selected.
            InitialiseAudioProjectData();

            // Add the Audio Project with empty events to the PackFile.
            AddAudioProjectToPackFile(_packFileService);

            // Load the custom States so that they can be referenced when the Event is loaded.
            //PrepareCustomStatesForComboBox(this);

            CloseWindowAction();
        }

        public void CreateAudioProjectDialogueEventsList()
        {
            _audioEditorViewModel.AudioProjectDialogueEvents.Clear();

            foreach (var checkBox in DialogueEventCheckBoxes)
            {
                if (checkBox.IsChecked == true)
                {
                    var dialogueEvent = checkBox.Content.ToString();
                    _audioEditorViewModel.AudioProjectDialogueEvents.Add(RemoveExtraUnderScoresFromString(dialogueEvent));
                }
            }
        }

        public void InitialiseAudioProjectData()
        {
            var settings = new Dictionary<string, object>
            {
                {"AudioProjectFileName", AudioProjectFileName},
                {"Language", SelectedLanguage},
                {"CustomStatesFilePath", CustomStatesFilePath}
            };

            AudioEditorInstance.AudioProjectData["Settings"] = [settings];

            foreach (var dialogueEvent in _audioEditorViewModel.AudioProjectDialogueEvents)
            {
                var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent];

                var dataGridItems = new List<Dictionary<string, object>>();
                var dataGridItem = new Dictionary<string, object>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupKey = AddExtraUnderScoresToString(stateGroupWithQualifier);
                    dataGridItem[stateGroupKey] = "";
                }

                dataGridItem["AudioFilesDisplay"] = "";
                dataGridItem["AudioFiles"] = "";

                dataGridItems.Add(dataGridItem);

                AudioEditorInstance.AudioProjectData[dialogueEvent] = dataGridItems;
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
            var recommendedEvents = AudioEditorSettings.FrontendVODialogueEvents.Where(e => e.Categories.Contains("Recommended")).Select(e => e.EventName).ToHashSet();

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
