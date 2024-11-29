using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.WindowHandling;
using static Editors.Audio.AudioEditor.AudioEditorViewModelHelpers;
using static Editors.Audio.AudioEditor.DynamicDataGrid;
using static Shared.Core.PackFiles.IPackFileService;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class CustomStatesDataGridProperties : ObservableObject
    {
        [ObservableProperty] private string _customVOActor;
        [ObservableProperty] private string _customVOCulture;
        [ObservableProperty] private string _customVOBattleSelection;
        [ObservableProperty] private string _customVOBattleSpecialAbility;
        [ObservableProperty] private string _customVOFactionLeader;
    }

    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IWindowFactory _windowFactory;
        private readonly IPackFileUiProvider _packFileUiProvider;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public string DisplayName { get; set; } = "Audio Editor";

        [ObservableProperty] private string _selectedAudioProjectEvent;
        [ObservableProperty] private bool _showCustomStatesOnly;

        // DataGrid data objects:
        public ObservableCollection<Dictionary<string, object>> AudioEditorDataGridItems { get; set; } = [];
        public ObservableCollection<CustomStatesDataGridProperties> CustomStatesDataGridItems { get; set; } = [];
        public static Dictionary<string, List<Dictionary<string, object>>> EventsData => AudioEditorData.Instance.EventsData; // Data storage for AudioEditorDataGridItems - managed in a single instance for ease of access.
        public static List<string> AudioProjectDialogueEvents => AudioEditorData.Instance.AudioProjectDialogueEvents;

        public AudioEditorViewModel(IAudioRepository audioRepository, IPackFileService packFileService, IWindowFactory windowFactory, IPackFileUiProvider packFileUiProvider)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _windowFactory = windowFactory;
            _packFileUiProvider = packFileUiProvider;
        }

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            AudioEditorData.Instance.SelectedAudioProjectEvent = value;

            // Load the Event upon selection.
            LoadEvent(_audioRepository, ShowCustomStatesOnly);
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            // Load the Event again to reset the ComboBoxes in the DataGrid.
            LoadEvent(_audioRepository, ShowCustomStatesOnly);
        }

        [RelayCommand] public void NewAudioProject()
        {
            var window = _windowFactory.Create<AudioEditorSettingsViewModel, AudioEditorSettingsView>("New Audio Project", 500, 410);
            window.AlwaysOnTop = false;
            window.ResizeMode = ResizeMode.NoResize;

            // Set the close action
            if (window.DataContext is AudioEditorSettingsViewModel viewModel)
                viewModel.SetCloseAction(() => window.Close());

            window.ShowWindow();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".json"]);
            if (result.Result)
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                EventsData.Clear();
                AudioEditorDataGridItems.Clear();
                SelectedAudioProjectEvent = "";

                // Create the object for State Groups with qualifiers so that their keys in the EventsData dictionary are unique.
                AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

                var filePath = _packFileService.GetFullPath(result.File);
                var file = _packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var eventData = AudioProjectData.ConvertAudioProjectToEventsData(_audioRepository, audioProjectJson);
                AudioEditorData.Instance.EventsData = eventData;
                _logger.Here().Information($"Loaded Audio Project file: {file.Name}");

                // Create the list of Events used in the Events ComboBox.
                //CreateAudioProjectEventsListFromAudioProject(this, EventsData);

                // Load the object which stores the custom States for use in the States ComboBox.
                PrepareCustomStatesForComboBox(this);
            }
        }

        [RelayCommand] public void SaveAudioProject()
        {
            UpdateEventDataWithCurrentEvent();

            AudioProjectData.AddAudioProjectToPackFile(_packFileService, EventsData, "dummy_name");
        }

        [RelayCommand] public void LoadCustomStates()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".json"]);
            if (result.Result)
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                CustomStatesDataGridItems.Clear();

                var filePath = _packFileService.GetFullPath(result.File);
                var file = _packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var customStatesFileData = JsonConvert.DeserializeObject<List<CustomStatesDataGridProperties>>(str);
                _logger.Here().Information($"Loaded Custom States file: {file.Name}");

                foreach (var customState in customStatesFileData)
                    CustomStatesDataGridItems.Add(customState);

                // Load the object which stores the custom States for use in the States ComboBox.
                PrepareCustomStatesForComboBox(this);

                // Reload the selected Event so the ComboBoxes are updated.
                LoadEvent(_audioRepository, ShowCustomStatesOnly);
            }
        }

        [RelayCommand] public void SaveCustomStates()
        {
            var dataGridItemsJson = JsonConvert.SerializeObject(CustomStatesDataGridItems, Formatting.Indented);
            var editablePack = _packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
  
            var fileEntry = new NewPackFileEntry("AudioProjects", new PackFile($"{"dummy_name"}.json", new MemorySource(byteArray)));
            _packFileService.AddFilesToPack(editablePack, [fileEntry]);

            _logger.Here().Information($"Saved Custom States file: {"dummy_name"}");
        }

        public void LoadEvent(IAudioRepository audioRepository, bool showCustomStatesOnly)
        {
            if (string.IsNullOrEmpty(AudioEditorData.Instance.SelectedAudioProjectEvent))
                return;

            _logger.Here().Information($"Loading event: {AudioEditorData.Instance.SelectedAudioProjectEvent}");

            ConfigureDataGrid(this, audioRepository, showCustomStatesOnly);

            if (EventsData.ContainsKey(AudioEditorData.Instance.SelectedAudioProjectEvent))

                foreach (var statePath in EventsData[AudioEditorData.Instance.SelectedAudioProjectEvent])
                    AudioEditorDataGridItems.Add(statePath);
        }

        public static void InitialiseEventsData()
        {
            foreach (var dialogueEvent in AudioProjectDialogueEvents)
            {
                var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent];

                var dataGridItems = new List<Dictionary<string, object>>();
                var dataGridItem = new Dictionary<string, object>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupKey = AddExtraUnderScoresToString(stateGroupWithQualifier);
                    dataGridItem[stateGroupKey] = "";
                    dataGridItem["AudioFilesDisplay"] = "";
                    dataGridItem["AudioFiles"] = "";
                }

                dataGridItems.Add(dataGridItem);
                EventsData[dialogueEvent] = dataGridItems;
            }
        }

        [RelayCommand] public void AddStatePath()
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, object>();

            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupKey = AddExtraUnderScoresToString(stateGroupWithQualifier);
                newRow[stateGroupKey] = "";
            }

            newRow["AudioFilesDisplay"] = "";

            AudioEditorDataGridItems.Add(newRow);

            UpdateEventDataWithCurrentEvent();
        }

        public void RemoveStatePath(Dictionary<string, object> rowToRemove)
        {
            AudioEditorDataGridItems.Remove(rowToRemove);

            UpdateEventDataWithCurrentEvent();
        }

        public void UpdateEventDataWithCurrentEvent()
        {
            if (AudioEditorDataGridItems == null)
                return;

            if (SelectedAudioProjectEvent != null)
                EventsData[SelectedAudioProjectEvent] = new List<Dictionary<string, object>>(AudioEditorDataGridItems);
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
                        var fileNames = filePaths.Select(filePath => $"\"{System.IO.Path.GetFileName(filePath)}\"");
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

        public static void CreateAudioProjectEventsListFromAudioProject(Dictionary<string, List<Dictionary<string, object>>> eventsData)
        {
            AudioProjectDialogueEvents.Clear();

            foreach (var dialogueEvent in eventsData.Keys)
                AudioProjectDialogueEvents.Add(dialogueEvent);
        }

        public void Close()
        {
        }

    }
}
