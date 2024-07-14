using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public partial class AudioEditorViewModel : ObservableObject, IEditorViewModel
    {
        public readonly IAudioRepository AudioRepository;
        public readonly PackFileService PackFileService;

        public string _previousSelectedAudioProjectEvent;
        [ObservableProperty]
        private string _selectedAudioProjectEvent;
        [ObservableProperty]
        private bool _showCustomStatesOnly;

        // Audio Project settings
        [ObservableProperty]
        private string _selectedAudioProjectEventType;
        [ObservableProperty]
        private string _selectedAudioProjectEventSubtype;
        [ObservableProperty]
        private DialogueEventsPreset _selectedAudioProjectEventsPreset;

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

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            Debug.WriteLine($"_selectedAudioProjectEventType changed to: {value}");
            UpdateAudioProjectEventSubType(this);
        }

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            _previousSelectedAudioProjectEvent = SelectedAudioProjectEvent;
            Debug.WriteLine($"_selectedAudioProjectEvent changed to: {value}");
            AudioEditorData.Instance.SelectedAudioProjectEvent = value;

            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        partial void OnSelectedAudioProjectEventSubtypeChanged(string value)
        {
            Debug.WriteLine($"_selectedAudioProjectEventSubtype changed to: {value}");
        }

        partial void OnSelectedAudioProjectEventsPresetChanged(DialogueEventsPreset value)
        {
            Debug.WriteLine($"_selectedAudioProjectEventsPreset changed to: {value}");
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            Debug.WriteLine($"_showCustomStatesOnly changed to: {value}");
            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        public void CreateAudioProject()
        {
            EventsData.Clear();

            CreateAudioProjectDialogueEventsList(this);
            AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

            InitialiseEventData(this);

            AudioEditorViewModelHelpers.LoadCustomStates(this);

            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);

            AudioProjectData.AddAudioProjectToPackFile(PackFileService, EventsData);
        }

        public void LoadAudioProject()
        {
            EventsData.Clear();
            AudioEditorDataGridItems.Clear();
            _previousSelectedAudioProjectEvent = "";
            SelectedAudioProjectEvent = "";

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
            }

            AudioEditorViewModelHelpers.LoadCustomStates(this);

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

                AudioEditorViewModelHelpers.LoadCustomStates(this);

                LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
            }
        }

        public void SaveCustomStates()
        {
            var dataGridItemsJson = JsonConvert.SerializeObject(CustomStatesDataGridItems, Formatting.Indented);
            var pack = PackFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
            PackFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"custom_states.json", new MemorySource(byteArray)));

            AudioEditorViewModelHelpers.LoadCustomStates(this);

            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        public void AddStatePath()
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            // Create a new dictionary and initialize it with the necessary keys
            var newRow = new Dictionary<string, object>();

            // Get the state groups with qualifiers for the selected audio project event
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];

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
