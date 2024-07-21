using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
    public class CustomStatesDataGridProperties
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
        Essential
    }

    public partial class AudioEditorViewModel : ObservableObject, IEditorViewModel
    {
        public readonly IAudioRepository AudioRepository;
        public readonly PackFileService PackFileService;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        // Audio Project settings properties:
        [ObservableProperty] private string _audioProjectFileName = "my_audio_project.json";
        [ObservableProperty] private string _customStatesFileName = "my_custom_states.json";
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;
        [ObservableProperty] private DialogueEventsPreset _selectedAudioProjectEventsPreset;

        // Properties for the main DataGrid:
        [ObservableProperty] private string _selectedAudioProjectEvent;
        [ObservableProperty] private bool _showCustomStatesOnly;

        // DataGrid observable collections:
        public ObservableCollection<Dictionary<string, object>> AudioEditorDataGridItems { get; set; } = [];
        public ObservableCollection<CustomStatesDataGridProperties> CustomStatesDataGridItems { get; set; } = [];

        // Audio Project settings:
        public static List<string> AudioProjectEventType { get; set; } = AudioEditorSettings.EventType;
        public ObservableCollection<string> AudioProjectSubtypes { get; set; } = []; // Determined according to what Event Type is selected
        public ObservableCollection<string> AudioProjectDialogueEvents { get; set; } = []; // The list of events in the Audio Project.

        // Data storage for AudioEditorDataGridItems - managed in a single instance for ease of access.
        public static Dictionary<string, List<Dictionary<string, object>>> EventsData => AudioEditorData.Instance.EventsData;

        public AudioEditorViewModel(IAudioRepository audioRepository, PackFileService packFileService)
        {
            AudioRepository = audioRepository;
            PackFileService = packFileService;
        }

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            // Update the ComboBox for EventSubType upon EventType selection.
            UpdateAudioProjectEventSubType(this);
        }

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            AudioEditorData.Instance.SelectedAudioProjectEvent = value;

            // Load the Event upon selection.
            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            // Load the Event again to reset the ComboBoxes in the DataGrid.
            LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
        }

        [RelayCommand] public void CreateAudioProject()
        {
            // Remove any pre-existing data.
            EventsData.Clear();
            AudioEditorDataGridItems.Clear();
            SelectedAudioProjectEvent = "";

            // Create the object for State Groups with qualifiers so that their keys in the EventsData dictionary are unique.
            AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

            // Initialise EventsData according to the Audio Project settings selected.
            InitialiseEventsData(this);

            // Add the Audio Project with empty events to the PackFile.
            AudioProjectData.AddAudioProjectToPackFile(PackFileService, EventsData, AudioProjectFileName);

            // Load the custom States so that they can be referenced when the Event is loaded.
            PrepareCustomStatesForComboBox(this);
        }

        public void LoadAudioProject()
        {
            using var browser = new PackFileBrowserWindow(PackFileService, [".json"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                EventsData.Clear();
                AudioEditorDataGridItems.Clear();
                SelectedAudioProjectEvent = "";

                // Create the object for State Groups with qualifiers so that their keys in the EventsData dictionary are unique.
                AddQualifiersToStateGroups(AudioRepository.DialogueEventsWithStateGroups);

                var filePath = PackFileService.GetFullPath(browser.SelectedFile);
                var file = PackFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var eventData = AudioProjectData.ConvertAudioProjectToEventsData(AudioRepository, audioProjectJson);
                AudioEditorData.Instance.EventsData = eventData;
                _logger.Here().Information($"Loaded Audio Project: {file.Name}");

                // Create the list of Events used in the Events ComboBox.
                CreateAudioProjectEventsListFromAudioProject(this, EventsData);

                // Load the object which stores the custom States for use in the States ComboBox.
                PrepareCustomStatesForComboBox(this);
            }
        }

        public void SaveAudioProject()
        {
            UpdateEventDataWithCurrentEvent(this);

            AudioProjectData.AddAudioProjectToPackFile(PackFileService, EventsData, AudioProjectFileName);
        }

        public void LoadCustomStates()
        {
            using var browser = new PackFileBrowserWindow(PackFileService, [".json"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                CustomStatesDataGridItems.Clear();

                var filePath = PackFileService.GetFullPath(browser.SelectedFile);
                var file = PackFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var customStatesFileData = JsonConvert.DeserializeObject<List<CustomStatesDataGridProperties>>(str);
                _logger.Here().Information($"Loaded Custom States: {file.Name}");

                foreach (var customState in customStatesFileData)
                    CustomStatesDataGridItems.Add(customState);

                // Load the object which stores the custom States for use in the States ComboBox.
                PrepareCustomStatesForComboBox(this);

                // Reload the selected Event so the ComboBoxes are updated.
                LoadEvent(this, AudioRepository, ShowCustomStatesOnly);
            }
        }

        public void SaveCustomStates()
        {
            var dataGridItemsJson = JsonConvert.SerializeObject(CustomStatesDataGridItems, Formatting.Indented);
            var pack = PackFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
            PackFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"{CustomStatesFileName}.json", new MemorySource(byteArray)));
            _logger.Here().Information($"Saved Custom States: {CustomStatesFileName}");
        }

        [RelayCommand] public void AddStatePath()
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, object>();

            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupKey = AddExtraUnderScoresToStateGroup(stateGroupWithQualifier);
                newRow[stateGroupKey] = "";
            }

            newRow["AudioFilesDisplay"] = "";

            AudioEditorDataGridItems.Add(newRow);

            UpdateEventDataWithCurrentEvent(this);
        }

        public void RemoveStatePath(Dictionary<string, object> rowToRemove)
        {
            AudioEditorDataGridItems.Remove(rowToRemove);

            UpdateEventDataWithCurrentEvent(this);
        }

        [RelayCommand] public void AddCustomStatesRow()
        {
            var newRow = new CustomStatesDataGridProperties();
            CustomStatesDataGridItems.Add(newRow);
        }

        [RelayCommand] public void RemoveCustomStatesRow(CustomStatesDataGridProperties item)
        {
            if (item != null && CustomStatesDataGridItems.Contains(item))
                CustomStatesDataGridItems.Remove(item);
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

                if (eventsData.ContainsKey(AudioEditorData.Instance.SelectedAudioProjectEvent))
                {
                    var eventList = eventsData[AudioEditorData.Instance.SelectedAudioProjectEvent];

                    // Find the matching row to insert the AudioFiles data.
                    var matchingRow = eventList.FirstOrDefault(context =>
                        DictionaryEqualityComparer<string, object>.Default.Equals(context, dataGridRow));

                    if (matchingRow != null)
                    {
                        var fileNames = filePaths.Select(filePath => $"\"{Path.GetFileName(filePath)}\"");
                        var fileNamesString = string.Join(", ", fileNames);
                        var filePathsString = string.Join(", ", filePaths.Select(filePath => $"\"{filePath}\""));

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
