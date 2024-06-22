using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using SharpDX.Direct3D9;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public enum DialogueEventsPreset
    {
        None,
        All,
        Essential,
        Custom
    }

    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private ObservableCollection<string> _eventItems;
        private string _selectedAudioProjectEvent;
        private string _selectedAudioProjectEventType;
        private string _selectedAudioProjectEventSubtype;
        private DialogueEventsPreset _selectedAudioProjectEventsPreset;

        // UI Stuff
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public AudioEditorDataViewModel AudioEditor { get; }
        public AudioEditorSettingsViewModel AudioEditorSettings { get; }

        // Data
        public ObservableCollection<Dictionary<string, string>> DataGridItems { get; set; } = new ObservableCollection<Dictionary<string, string>>();
        public Dictionary<string, List<Dictionary<string, string>>> EventData { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
        public ObservableCollection<string> EventItems { get { return _eventItems; } set { SetAndNotify(ref _eventItems, value); } }
        public List<string> DialogueEvents { get; set; } = new List<string>();

        // Audio project settings display data
        public List<string> AudioProjectEventType { get; set; } = new List<string>();
        public ObservableCollection<string> AudioProjectSubTypes { get; set; } = new ObservableCollection<string>(); // ObservableCollection used as the list gets updated. If it was just List then the updated list wouldn't be passed to the UI.
        public ObservableCollection<string> AudioProjectDialogueEvents { get; set; } = new ObservableCollection<string>();
        public NotifyAttr<string> AudioProjectDialogueEventsText { get; set; } = new NotifyAttr<string>("");

        // Commands
        public ICommand AddStatePathCommand { get; private set; }
        public ICommand SaveEventCommand { get; private set; }
        public ICommand CreateAudioProject { get; private set; }

        public AudioEditorViewModel(AudioEditorDataViewModel audioEditor, AudioEditorSettingsViewModel audioEditorSettings, IAudioRepository audioRepository)
        {
            // View Model parameters
            AudioEditor = audioEditor;
            AudioEditorSettings = audioEditorSettings;
            _audioRepository = audioRepository;

            // Link functions to commands
            AddStatePathCommand = new RelayCommand(AddStatePath);
            SaveEventCommand = new RelayCommand(SaveEvent);
            CreateAudioProject = new RelayCommand(() => PrepareAudioProject(EventData));

            // Data
            EventItems = new ObservableCollection<string>(EventData.Keys);

            // Audio project settings display data
            var audioProjectSettings = new AudioProjectSettings();
            AudioProjectEventType = audioProjectSettings.EventType;
        }

        // Get the value selected from Event Type ComboBox which has the binding SelectedAudioProjectEventType and update the Event Subtype ComboBox.
        public string SelectedAudioProjectEventType
        {
            get { return _selectedAudioProjectEventType; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEventType changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventType, value);
                AudioEditorViewModelHelpers.UpdateAudioProjectEventSubType(this);
                AudioEditorViewModelHelpers.UpdateAudioProjectDialogueEvents(this);
            }
        }

        public string SelectedAudioProjectEventSubtype
        {
            get { return _selectedAudioProjectEventSubtype; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEventSubtype changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventSubtype, value);
                AudioEditorViewModelHelpers.UpdateAudioProjectDialogueEvents(this);
            }
        }

        public DialogueEventsPreset SelectedDialogueEventsPreset
        {
            get { return _selectedAudioProjectEventsPreset; }
            set
            {
                Debug.WriteLine($"SelectedDialogueEventsPreset changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventsPreset, value);
                AudioEditorViewModelHelpers.UpdateAudioProjectDialogueEvents(this);
            }
        }

        public string SelectedAudioProjectEvent
        {
            get { return _selectedAudioProjectEvent; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEvent changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEvent, value);
                EditEvent();
            }
        }

        private void EditEvent()
        {
            if (string.IsNullOrEmpty(_selectedAudioProjectEvent))
                return;

            AudioEditorViewModelHelpers.AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);
            AudioEditorViewModelHelpers.ConfigureDataGrid(_audioRepository, _selectedAudioProjectEvent, DataGridItems);
        }

        private void AddStatePath()
        {
            if (string.IsNullOrEmpty(_selectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, string>();
            DataGridItems.Add(newRow);
        }

        private void SaveEvent()
        {
            if (!EventData.ContainsKey(_selectedAudioProjectEvent))
                EventData[_selectedAudioProjectEvent] = new List<Dictionary<string, string>>();

            // Add each item from DataGridItems to the EventData list for the selected event
            foreach (var item in DataGridItems)
                EventData[_selectedAudioProjectEvent].Add(new Dictionary<string, string>(item));

            var dataGridItemsJson = JsonConvert.SerializeObject(DataGridItems, Formatting.Indented);
            var eventDataJson = JsonConvert.SerializeObject(EventData, Formatting.Indented);
            Debug.WriteLine($"dataGridItems: {dataGridItemsJson}");
            Debug.WriteLine($"eventData: {eventDataJson}");

            // Update EventItems after modifying EventData
            EventItems = new ObservableCollection<string>(EventData.Keys);
        }

        private void PrepareAudioProject(Dictionary<string, List<Dictionary<string, string>>> eventData)
        {
            // Create an instance of AudioProject
            var audioProject = new AudioProject();

            // Set BnkName and Language settings
            audioProject.Settings.BnkName = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak";
            audioProject.Settings.Language = "english(uk)";

            foreach (var eventName in eventData.Keys)
            {
                var eventItems = eventData[eventName];

                foreach (var eventItem in eventItems)
                    audioProject.AddAudioEditorItem(eventName, eventItem);
            }

            var audioProjectjson = AudioProjectSerialisation.ConvertToAudioProject(audioProject);
            Debug.WriteLine($"eventData: {audioProjectjson}");
        }



















        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }
}
