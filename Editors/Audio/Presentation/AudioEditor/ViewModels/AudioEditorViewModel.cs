using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly PackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private string _selectedEventName;
        private ObservableCollection<string> _eventItems;
        private string _filterText;
        private string _selectedEvent;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public ObservableCollection<Dictionary<string, string>> DataGridItems { get; set; } = new ObservableCollection<Dictionary<string, string>>();
        public Dictionary<string, List<Dictionary<string, string>>> EventData { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
        public EventSelectionFilter EventFilter { get; private set; }
        public ObservableCollection<string> EventItems { get { return _eventItems; } set { SetAndNotify(ref _eventItems, value); } }
        public string FilterText { get { return _filterText; } set { SetAndNotify(ref _filterText, value); } }
        public string SelectedEvent { get { return _selectedEvent; } set { SetAndNotify(ref _selectedEvent, value); OnSelectedEventChanged(value); } }

        public ICommand EditEventCommand { get; private set; }
        public ICommand AddStatePathCommand { get; private set; }
        public ICommand SaveEventCommand { get; private set; }
        public ICommand CreateAudioProject { get; private set; }

        public AudioEditorViewModel(PackFileService packFileService, IAudioRepository audioRepository)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;

            EditEventCommand = new RelayCommand(EditEvent);
            AddStatePathCommand = new RelayCommand(AddStatePath);
            SaveEventCommand = new RelayCommand(SaveEvent);
            CreateAudioProject = new RelayCommand(() => PrepareAudioProject(EventData));

            EventFilter = new EventSelectionFilter(_audioRepository, false, true);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;

            EventItems = new ObservableCollection<string>(EventData.Keys);
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        public bool Save()
        {
            return true;
        }

        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;

        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue == null)
                return;

            _selectedEventName = newValue.DisplayName.ToString();
            Debug.WriteLine($"selectedEventName: {_selectedEventName}");
        }

        private void OnSelectedEventChanged(string newValue)
        {
            // Custom logic when SelectedEvent changes
            if (!DisableCallbacks)
            {
                // Handle selected event change logic here
                Debug.WriteLine($"Selected Event: {newValue}");
            }
        }

        private void EditEvent()
        {
            if (string.IsNullOrEmpty(_selectedEventName))
                return;

            AudioEditorViewModelHelpers.AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);
            AudioEditorViewModelHelpers.ConfigureDataGrid(_audioRepository, _selectedEventName, DataGridItems);
        }

        private void AddStatePath()
        {
            if (string.IsNullOrEmpty(_selectedEventName))
                return;

            var newRow = new Dictionary<string, string>();
            DataGridItems.Add(newRow);
        }

        private void SaveEvent()
        {
            if (!EventData.ContainsKey(_selectedEventName))
                EventData[_selectedEventName] = new List<Dictionary<string, string>>();

            // Add each item from DataGridItems to the EventData list for the selected event
            foreach (var item in DataGridItems)
                EventData[_selectedEventName].Add(new Dictionary<string, string>(item));

            var dataGridItemsJson = JsonConvert.SerializeObject(DataGridItems, Formatting.Indented);
            var eventDataJson = JsonConvert.SerializeObject(EventData, Formatting.Indented);
            Debug.WriteLine($"dataGridItems: {dataGridItemsJson}");
            Debug.WriteLine($"eventData: {eventDataJson}");

            // Update EventItems after modifying EventData
            EventItems = new ObservableCollection<string>(EventData.Keys);
        }

        private static void PrepareAudioProject(Dictionary<string, List<Dictionary<string, string>>> eventData)
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
    }
}
