using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.Input;
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
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private string _selectedAudioProjectEvent;
        private string _previousSelectedAudioProjectEvent;
        private string _selectedAudioProjectEventType;
        private string _selectedAudioProjectEventSubtype;
        private DialogueEventsPreset _selectedAudioProjectEventsPreset;

        // UI Stuff
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public AudioEditorDataViewModel AudioEditor { get; }
        public AudioEditorSettingsViewModel AudioEditorSettings { get; }

        // Data
        public ObservableCollection<Dictionary<string, string>> AudioEditorDataGridItems { get; set; } = new ObservableCollection<Dictionary<string, string>>();
        public ObservableCollection<DataGridRowModel> CustomStatesDataGridItems { get; set; } = new ObservableCollection<DataGridRowModel>();
        public Dictionary<string, List<Dictionary<string, string>>> EventData { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();

        // Audio project settings display data
        public List<string> AudioProjectEventType { get; set; } = new List<string>();
        public ObservableCollection<string> AudioProjectSubTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AudioProjectDialogueEvents { get; set; } = new ObservableCollection<string>();
        public NotifyAttr<string> AudioProjectDialogueEventsText { get; set; } = new NotifyAttr<string>("");

        // Commands
        public ICommand AddStatePathCommand { get; set; }
        public ICommand CreateAudioProjectCommand { get; set; }
        public ICommand SaveCustomStatesCommand { get; set; }
        public ICommand LoadCustomStatesFileCommand { get; set; }

        public AudioEditorViewModel(AudioEditorDataViewModel audioEditor, AudioEditorSettingsViewModel audioEditorSettings, IAudioRepository audioRepository, PackFileService packFileService)
        {
            // View Model parameters
            AudioEditor = audioEditor;
            AudioEditorSettings = audioEditorSettings;
            _audioRepository = audioRepository;
            _packFileService = packFileService;

            // Command functions
            AddStatePathCommand = new RelayCommand(AddStatePath);
            CreateAudioProjectCommand = new RelayCommand(CreateAudioProject);
            SaveCustomStatesCommand = new RelayCommand(SaveCustomStates);
            LoadCustomStatesFileCommand = new RelayCommand(LoadCustomStates);

            // Audio project settings display data
            var audioProjectSettings = new AudioProjectSettings();
            AudioProjectEventType = audioProjectSettings.EventType;
        }

        public string SelectedAudioProjectEventType
        {
            get { return _selectedAudioProjectEventType; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEventType changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventType, value);
                AudioEditorViewModelHelpers.UpdateAudioProjectEventSubType(this);
            }
        }

        public string SelectedAudioProjectEventSubtype
        {
            get { return _selectedAudioProjectEventSubtype; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEventSubtype changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventSubtype, value);
            }
        }

        public DialogueEventsPreset SelectedDialogueEventsPreset
        {
            get { return _selectedAudioProjectEventsPreset; }
            set
            {
                Debug.WriteLine($"SelectedDialogueEventsPreset changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventsPreset, value);
            }
        }

        public string SelectedAudioProjectEvent
        {
            get { return _selectedAudioProjectEvent; }
            set
            {
                if (_selectedAudioProjectEvent != value)
                {
                    _previousSelectedAudioProjectEvent = _selectedAudioProjectEvent;
                    Debug.WriteLine($"_selectedAudioProjectEvent changed from: {_previousSelectedAudioProjectEvent} to: {value}");
                    SetAndNotify(ref _selectedAudioProjectEvent, value);
                    LoadEvent(false);
                }
            }
        }

        public void CreateAudioProject()
        {
            EventData.Clear();

            AudioEditorViewModelHelpers.CreateAudioProjectDialogueEventsList(this);

            foreach (var dialogueEvent in AudioProjectDialogueEvents)
            {
                if (!EventData.ContainsKey(dialogueEvent))
                    EventData[dialogueEvent] = new List<Dictionary<string, string>>();

                EventData[dialogueEvent].Add(new Dictionary<string, string>());
            }

            LoadEvent();
        }

        public void LoadAudioProject()
        {
            EventData.Clear();
            AudioEditorDataGridItems.Clear();

            using var browser = new PackFileBrowserWindow(_packFileService, [".json"]);

            if (browser.ShowDialog())
            {
                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
                var packFile = _packFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);

                var eventData = AudioProjectData.ConvertFromAudioProject(_audioRepository, audioProjectJson);
                EventData = eventData;

                LoadEvent();
            }

            AudioEditorViewModelHelpers.CreateAudioProjectDialogueEventsListFromAudioProject(this, EventData);
        }

        public void SaveAudioProject()
        {
            UpdateEventDataWithCurrentEvent();
            AudioProjectData.ProcessAudioProject(_packFileService, EventData);
        }

        public void LoadCustomStates()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".json"]);

            if (browser.ShowDialog())
            {
                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
                var packFile = _packFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var customStatesFileData = JsonConvert.DeserializeObject<List<DataGridRowModel>>(str);

                CustomStatesDataGridItems.Clear();

                foreach (var customState in customStatesFileData)
                    CustomStatesDataGridItems.Add(customState);         
            }
        }

        public void SaveCustomStates()
        {
            var dataGridItemsJson = JsonConvert.SerializeObject(CustomStatesDataGridItems, Formatting.Indented);
            var pack = _packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
            _packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"custom_states.json", new MemorySource(byteArray)));
        }


        public void AddStatePath()
        {
            if (string.IsNullOrEmpty(_selectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, string>();
            AudioEditorDataGridItems.Add(newRow);
        }

        public void LoadEvent(bool isLoadingAudioProject = true)
        {
            if (string.IsNullOrEmpty(_selectedAudioProjectEvent))
                return;

            if (!isLoadingAudioProject)
                UpdateEventDataWithPreviousEvent();

            AudioEditorViewModelHelpers.AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);
            AudioEditorViewModelHelpers.ConfigureDataGrid(_audioRepository, _selectedAudioProjectEvent, AudioEditorDataGridItems);

            if (EventData.ContainsKey(_selectedAudioProjectEvent))
            {
                foreach (var statePath in EventData[_selectedAudioProjectEvent])
                    AudioEditorDataGridItems.Add(statePath);
            }
        }

        public void UpdateEventDataWithPreviousEvent()
        {
            if (AudioEditorDataGridItems == null)
                return;

            if (_previousSelectedAudioProjectEvent != null)
                EventData[_previousSelectedAudioProjectEvent] = new List<Dictionary<string, string>>(AudioEditorDataGridItems);
        }

        public void UpdateEventDataWithCurrentEvent()
        {
            if (AudioEditorDataGridItems == null)
                return;

            if (_selectedAudioProjectEvent != null)
                EventData[_selectedAudioProjectEvent] = new List<Dictionary<string, string>>(AudioEditorDataGridItems);
        }

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }

    public class DataGridRowModel
    {
        public string CustomVOActor { get; set; }
        public string CustomVOCulture { get; set; }
        public string CustomVOBattleSelection { get; set; }
        public string CustomVOBattleSpecialAbility { get; set; }
        public string CustomVOFactionLeader { get; set; }
    }

    public enum DialogueEventsPreset
    {
        None,
        All,
        Essential,
        Custom
    }
}
