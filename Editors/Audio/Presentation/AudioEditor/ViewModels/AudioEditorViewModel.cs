using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using static Editors.Audio.Presentation.AudioEditor.AudioEditorViewModelHelpers;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public class CustomStateDataGridProperties
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

    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public readonly IAudioRepository AudioRepository;
        public readonly PackFileService PackFileService;
        public string _selectedAudioProjectEvent;
        public string _previousSelectedAudioProjectEvent;
        public string _selectedAudioProjectEventType;
        public string _selectedAudioProjectEventSubtype;
        public DialogueEventsPreset _selectedAudioProjectEventsPreset;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public AudioEditorDataViewModel AudioEditor { get; }
        public AudioEditorSettingsViewModel AudioEditorSettings { get; }

        // DataGrid observable collections.
        public ObservableCollection<Dictionary<string, object>> AudioEditorDataGridItems { get; set; } = [];
        public ObservableCollection<CustomStateDataGridProperties> CustomStatesDataGridItems { get; set; } = [];

        // Data storage for AudioEditorDataGridItems managed in a single instance for ease of access.
        public static Dictionary<string, List<Dictionary<string, object>>> EventsData => AudioEditorData.Instance.EventsData;

        // Audio Project settings.
        public List<string> AudioProjectEventType { get; set; } = [];
        public ObservableCollection<string> AudioProjectSubTypes { get; set; } = [];

        // The list of events in the Audio Project.
        public ObservableCollection<string> AudioProjectDialogueEvents { get; set; } = [];

        public ICommand AddStatePathCommand { get; set; }
        public ICommand CreateAudioProjectCommand { get; set; }

        public AudioEditorViewModel(AudioEditorDataViewModel audioEditor, AudioEditorSettingsViewModel audioEditorSettings, IAudioRepository audioRepository, PackFileService packFileService)
        {
            AudioEditor = audioEditor;
            AudioEditorSettings = audioEditorSettings;
            AudioRepository = audioRepository;
            PackFileService = packFileService;

            AddStatePathCommand = new RelayCommand(AddStatePath);
            CreateAudioProjectCommand = new RelayCommand(CreateAudioProject);

            // Audio project settings.
            var audioProjectSettings = new AudioEditorSettings();
            AudioProjectEventType = audioProjectSettings.EventType;
        }

        public string SelectedAudioProjectEventType
        {
            get { return _selectedAudioProjectEventType; }
            set
            {
                Debug.WriteLine($"_selectedAudioProjectEventType changed to: {value}");
                SetAndNotify(ref _selectedAudioProjectEventType, value);
                UpdateAudioProjectEventSubType(this);
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
                    AudioEditorData.Instance.SelectedAudioProjectEvent = value;
                    LoadEvent(this, false, AudioRepository);
                }
            }
        }

        public void CreateAudioProject()
        {
            EventsData.Clear();

            CreateAudioProjectDialogueEventsList(this);
            AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

            InitialiseEventData(this);

            LoadEvent(this, true, AudioRepository);

            AudioProjectData.AddAudioProjectToPackFile(PackFileService, EventsData);
        }

        public void LoadAudioProject()
        {
            EventsData.Clear();
            AudioEditorDataGridItems.Clear();
            _previousSelectedAudioProjectEvent = "";
            _selectedAudioProjectEvent = "";

            AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

            using var browser = new PackFileBrowserWindow(PackFileService, [".json"]);

            if (browser.ShowDialog())
            {
                var filePath = PackFileService.GetFullPath(browser.SelectedFile);
                var packFile = PackFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);

                var eventData = AudioProjectData.ConvertAudioProjectToEventData(AudioRepository, audioProjectJson);
                AudioEditorData.Instance.EventsData = eventData;

                LoadEvent(this, true, AudioRepository);
            }

            CreateAudioProjectDialogueEventsListFromAudioProject(this, EventsData);
        }

        public void SaveAudioProject()
        {
            UpdateEventDataWithCurrentEvent(this);
            AudioProjectData.AddAudioProjectToPackFile(PackFileService, EventsData);
        }

        public void LoadCustomStates()
        {
            using var browser = new PackFileBrowserWindow(PackFileService, [".json"]);

            if (browser.ShowDialog())
            {
                var filePath = PackFileService.GetFullPath(browser.SelectedFile);
                var packFile = PackFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var customStatesFileData = JsonConvert.DeserializeObject<List<CustomStateDataGridProperties>>(str);

                CustomStatesDataGridItems.Clear();

                foreach (var customState in customStatesFileData)
                    CustomStatesDataGridItems.Add(customState);         
            }
        }

        public void SaveCustomStates()
        {
            var dataGridItemsJson = JsonConvert.SerializeObject(CustomStatesDataGridItems, Formatting.Indented);
            var pack = PackFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
            PackFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"custom_states.json", new MemorySource(byteArray)));
        }

        public void AddStatePath()
        {
            if (string.IsNullOrEmpty(_selectedAudioProjectEvent))
                return;

            // Create a new dictionary and initialize it with the necessary keys
            var newRow = new Dictionary<string, object>();

            // Get the state groups with qualifiers for the selected audio project event
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[_selectedAudioProjectEvent];

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupKey = AddExtraUnderScoresToStateGroup(stateGroupWithQualifier);
                newRow[stateGroupKey] = "";
            }

            // Add the Sounds key
            newRow["AudioFilesDisplay"] = "";

            // Add the new dictionary to AudioEditorDataGridItems
            AudioEditorDataGridItems.Add(newRow);

            // Update the event data with the current event
            UpdateEventDataWithCurrentEvent(this);
        }

        public void RemoveStatePath(Dictionary<string, object> rowToRemove)
        {
            AudioEditorDataGridItems.Remove(rowToRemove);
            UpdateEventDataWithCurrentEvent(this);
        }

        public static void AddAudioFiles(Dictionary<string, object> dataGridRow, System.Windows.Controls.TextBox textBox)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Multiselect = true,
                Filter = "WAV files (*.wav)|*.wav"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePaths = dialog.FileNames;
                var eventsData = AudioEditorData.Instance.EventsData;

                // Check if SelectedAudioProjectEvent exists as a key in EventData
                if (eventsData.ContainsKey(AudioEditorData.Instance.SelectedAudioProjectEvent))
                {
                    var eventList = eventsData[AudioEditorData.Instance.SelectedAudioProjectEvent];

                    // Find the matching rowContext dictionary in eventList
                    var matchingRow = eventList.FirstOrDefault(context =>
                        DictionaryEqualityComparer<string, object>.Default.Equals(context, dataGridRow));

                    if (matchingRow != null)
                    {
                        // Update the TextBox text with the selected file names
                        var fileNames = filePaths.Select(filePath => $"\"{Path.GetFileName(filePath)}\"");
                        var fileNamesString = string.Join(", ", fileNames);
                        var filePathsString = string.Join(", ", filePaths.Select(filePath => $"\"{filePath}\""));

                        // Add the sounds list as another object in the matching rowContext dictionary
                        matchingRow["AudioFilesDisplay"] = filePaths.ToList();
                        matchingRow["AudioFiles"] = filePaths.ToList();

                        textBox.Text = fileNamesString;
                        textBox.ToolTip = filePathsString;
                    }
                }
            }
        }

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
