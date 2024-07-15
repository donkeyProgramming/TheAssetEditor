using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Serilog;
using Shared.Core.ErrorHandling;
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

    public partial class AudioEditorViewModel(IAudioRepository audioRepository, PackFileService packFileService) : ObservableObject, IEditorViewModel
    {
        public readonly IAudioRepository AudioRepository = audioRepository;
        public readonly PackFileService PackFileService = packFileService;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        // Properties for the main DataGrid.
        [ObservableProperty] private string _selectedAudioProjectEvent;
        [ObservableProperty] private bool _showCustomStatesOnly;

        // Audio Project settings properties.
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;
        [ObservableProperty] private DialogueEventsPreset _selectedAudioProjectEventsPreset;

        // DataGrid observable collections:
        public ObservableCollection<Dictionary<string, object>> AudioEditorDataGridItems { get; set; } = [];
        public ObservableCollection<CustomStateDataGridProperties> CustomStatesDataGridItems { get; set; } = [];

        // Audio Project settings:
        public static List<string> AudioProjectEventType { get; set; } = AudioEditorSettings.EventType;
        public ObservableCollection<string> AudioProjectSubTypes { get; set; } = []; // Determined according to what Event Type is selected
        public ObservableCollection<string> AudioProjectDialogueEvents { get; set; } = []; // The list of events in the Audio Project.

        // Data storage for AudioEditorDataGridItems managed in a single instance for ease of access.
        public static Dictionary<string, List<Dictionary<string, object>>> EventsData => AudioEditorData.Instance.EventsData;

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            UpdateAudioProjectEventSubType(this);
        }

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            AudioEditorData.Instance.SelectedAudioProjectEvent = value;
            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        [RelayCommand] public void CreateAudioProject()
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
            SelectedAudioProjectEvent = "";

            AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

            using var browser = new PackFileBrowserWindow(PackFileService, [".json"]);

            if (browser.ShowDialog())
            {
                var filePath = PackFileService.GetFullPath(browser.SelectedFile);
                var packFile = PackFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                _logger.Here().Information($"Loading Audio Project: {filePath}");

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

        [RelayCommand] public void AddStatePath()
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
