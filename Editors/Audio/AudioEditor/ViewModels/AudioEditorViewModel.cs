using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Newtonsoft.Json;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.WindowHandling;
using static Editors.Audio.AudioEditor.AudioEditorData;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProjectConverter;
using static Editors.Audio.AudioEditor.DynamicDataGrid;

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

    public partial class AudioEditorViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly IWindowFactory _windowFactory;
        private readonly SoundPlayer _soundPlayer;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        [ObservableProperty] private string _selectedAudioProjectEvent;
        [ObservableProperty] private bool _showCustomStatesOnly;
        [ObservableProperty] private ObservableCollection<string> _audioProjectDialogueEvents = [];

        public ObservableCollection<Dictionary<string, object>> AudioEditorDataGridItems { get; set; } = [];

        public ObservableCollection<CustomStatesDataGridProperties> CustomStatesDataGridItems { get; set; } = [];

        public AudioEditorViewModel(IAudioRepository audioRepository, PackFileService packFileService, IWindowFactory windowFactory, SoundPlayer soundPlayer)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _windowFactory = windowFactory;
            _soundPlayer = soundPlayer;
        }

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            AudioEditorInstance.SelectedAudioProjectEvent = value;

            // Load the Event upon selection.
            LoadEvent(_audioRepository, ShowCustomStatesOnly);
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            // Load the Event again to reset the ComboBoxes in the DataGrid.
            LoadEvent(_audioRepository, ShowCustomStatesOnly);
        }

        [RelayCommand] public void NewVOAudioProject()
        {
            var window = _windowFactory.Create<AudioEditorNewAudioProjectViewModel, AudioEditorNewAudioProjectView>("New Audio Project", 560, 470);
            window.AlwaysOnTop = false;
            window.ResizeMode = ResizeMode.NoResize;

            // Set the close action
            if (window.DataContext is AudioEditorNewAudioProjectViewModel viewModel)
                viewModel.SetCloseAction(() => window.Close());

            window.ShowWindow();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".json"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                AudioEditorInstance.AudioProjectData.Clear();
                AudioEditorDataGridItems.Clear();
                SelectedAudioProjectEvent = "";

                // Create the object for State Groups with qualifiers so that their keys in the AudioProjectConverter dictionary are unique.
                AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
                var file = _packFileService.FindFile(filePath);
                var fileName = Path.GetFileNameWithoutExtension(file.Name);

                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var audioProjectData = ConvertFromAudioProjectJson(_audioRepository, audioProjectJson);

                AudioEditorInstance.AudioProjectData = audioProjectData;
                AudioEditorInstance.AudioProjectFileName = fileName;

                _logger.Here().Information($"Loaded Audio Project file: {fileName}");

                // Create the list of Events used in the Events ComboBox.
                CreateAudioProjectEventsListFromAudioProject();

                // Load the object which stores the custom States for use in the States ComboBox.
                PrepareCustomStatesForComboBox(this);
            }
        }

        [RelayCommand] public void SaveAudioProject()
        {
            UpdateEventDataWithCurrentEvent();

            AddAudioProjectToPackFile(_packFileService);
        }

        [RelayCommand] public void LoadCustomStates()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".json"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                CustomStatesDataGridItems.Clear();

                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
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
            var pack = _packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(dataGridItemsJson);
            _packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"{AudioEditorInstance.AudioProjectFileName}.json", new MemorySource(byteArray)));
            _logger.Here().Information($"Saved Custom States file: {AudioEditorInstance.AudioProjectFileName}");
        }

        public void LoadEvent(IAudioRepository audioRepository, bool showCustomStatesOnly)
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            _logger.Here().Information($"Loading event: {SelectedAudioProjectEvent}");

            ConfigureDataGrid(this, audioRepository, showCustomStatesOnly);

            if (AudioEditorInstance.AudioProjectData.ContainsKey(SelectedAudioProjectEvent))

                foreach (var statePath in AudioEditorInstance.AudioProjectData[SelectedAudioProjectEvent])
                    AudioEditorDataGridItems.Add(statePath);
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
                AudioEditorInstance.AudioProjectData[SelectedAudioProjectEvent] = new List<Dictionary<string, object>>(AudioEditorDataGridItems);
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

                if (AudioEditorInstance.AudioProjectData.ContainsKey(AudioEditorInstance.SelectedAudioProjectEvent))
                {
                    var eventList = AudioEditorInstance.AudioProjectData[AudioEditorInstance.SelectedAudioProjectEvent];

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

        public void CreateAudioProjectEventsListFromAudioProject()
        {
            AudioProjectDialogueEvents.Clear();

            foreach (var dialogueEvent in AudioEditorInstance.AudioProjectData.Keys)
                AudioProjectDialogueEvents.Add(dialogueEvent);
        }

        public void ResetAudioEditorViewModelData()
        {
            SelectedAudioProjectEvent = null;
            ShowCustomStatesOnly = false;
            AudioProjectDialogueEvents.Clear();
            AudioEditorDataGridItems.Clear();
            CustomStatesDataGridItems.Clear();
        }

        public void Close()
        {
            ResetAudioEditorViewModelData();
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
