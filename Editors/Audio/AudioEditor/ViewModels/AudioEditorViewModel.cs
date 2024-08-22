using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Microsoft.Win32;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.WindowHandling;
using static Editors.Audio.AudioEditor.AudioEditorData;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProjectData;
using static Editors.Audio.AudioEditor.DialogueEventDynamicDataGrid;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly IWindowFactory _windowFactory;
        private readonly SoundPlayer _soundPlayer;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        // Controls for the user.
        [ObservableProperty] private string _selectedAudioProjectEvent;
        [ObservableProperty] private bool _showCustomStatesOnly;

        // The DataGrid data.
        [ObservableProperty] private ObservableCollection<string> _audioProjectEvents = [];
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _dataGridData = [];

        public AudioEditorViewModel(IAudioRepository audioRepository, PackFileService packFileService, IWindowFactory windowFactory, SoundPlayer soundPlayer)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _windowFactory = windowFactory;
            _soundPlayer = soundPlayer;

            DataGridData.CollectionChanged += OndataGridDataChanged;
        }

        private void OndataGridDataChanged(object dataGridData, NotifyCollectionChangedEventArgs e)
        {
            // May use later...
        }

        partial void OnSelectedAudioProjectEventChanged(string oldValue, string newValue)
        {
            AudioEditorInstance.SelectedAudioProjectEvent = newValue;
            AudioEditorInstance.PreviousSelectedAudioProjectEvent = oldValue;

            // Save the DataGridData from the Event that was just being worked (PreviousSelectedAudioProjectEvent) on to the Audio Project.
            ConvertDataGridDataToAudioProject(DataGridData, AudioEditorInstance.PreviousSelectedAudioProjectEvent);

            // Load the Event upon selection.
            LoadDialogueEvent(_audioRepository, ShowCustomStatesOnly);
        }

        partial void OnShowCustomStatesOnlyChanged(bool value)
        {
            // Load the Event again to reset the ComboBoxes in the DataGrid.
            LoadDialogueEvent(_audioRepository, ShowCustomStatesOnly);
        }

        [RelayCommand] public void NewVOAudioProject()
        {
            var window = _windowFactory.Create<NewVOAudioProjectViewModel, NewVOAudioProjectView>("New Audio Project", 560, 500);
            window.AlwaysOnTop = false;
            window.ResizeMode = ResizeMode.NoResize;

            // Set the close action
            if (window.DataContext is NewVOAudioProjectViewModel viewModel)
                viewModel.SetCloseAction(() => window.Close());

            window.ShowWindow();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".audioproject"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                ResetAudioEditorViewModelData();
                AudioEditorInstance.ResetAudioEditorData();

                // Create the object for State Groups with qualifiers so that their keys in the AudioProject dictionary are unique.
                AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

                var filePath = browser.SelectedPath;
                var file = _packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var audioProject = JsonSerializer.Deserialize<AudioProject>(audioProjectJson);

                // Set the data.
                AudioEditorInstance.AudioProject = audioProject;

                var audioProjectFileName = AudioEditorInstance.AudioProject.Settings.AudioProjectName;
                _logger.Here().Information($"Loaded Audio Project file: {audioProjectFileName}");

                // Create the list of Events used in the Events ComboBox.
                CreateAudioProjectEventsListFromAudioProject();

                // Load the object which stores the custom States for use in the States ComboBox.
                //PrepareCustomStatesForComboBox(this);
            }
        }

        [RelayCommand] public void SaveAudioProject()
        {
            ConvertDataGridDataToAudioProject(DataGridData, SelectedAudioProjectEvent);

            AddAudioProjectToPackFile(_packFileService);
        }

        public void LoadDialogueEvent(IAudioRepository audioRepository, bool showCustomStatesOnly)
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            // Configure the DataGrid before adding the data to it so that once you add the data it populates.
            ConfigureDataGrid(this, audioRepository, showCustomStatesOnly);

            var audioProject = AudioEditorInstance.AudioProject;
            var dialogueEvent = audioProject.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == SelectedAudioProjectEvent);
            var decisionTree = dialogueEvent.DecisionTree;

            DataGridData.Clear();

            if (decisionTree.Count > 0)
                ConvertAudioProjecToDataGridData(DataGridData, audioProject, SelectedAudioProjectEvent);

            else
                InitialiseDataGridData(decisionTree);

            _logger.Here().Information($"Loaded event: {SelectedAudioProjectEvent}");
        }

        private void InitialiseDataGridData(List<DecisionNode> decisionTree)
        {
            // Process Decision Tree and populate the objects in dataGridData. Make sure to add the kvp for the column header i.e. the stateGroupWithQualifier (with extra underscores) to prevent a binding error. 
            foreach (var statePath in decisionTree)
            {
                var filePaths = statePath.AudioFiles;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                var dataGridRow = new Dictionary<string, object>
                {
                    ["AudioFiles"] = new List<string>(statePath.AudioFiles), // Create a new instance of the list to avoid referencing the original collection.
                    ["AudioFilesDisplay"] = fileNamesString
                };

                if (DialogueEventsWithStateGroupsWithQualifiers.TryGetValue(SelectedAudioProjectEvent, out var stateGroupsWithQualifiers))
                {
                    foreach (var kvp in stateGroupsWithQualifiers)
                    {
                        var stateGroupWithQualifier = kvp.Key;
                        dataGridRow[AddExtraUnderscoresToString(stateGroupWithQualifier)] = string.Empty;
                    }
                }

                DataGridData.Add(dataGridRow);
            }
        }

        [RelayCommand] public void AddStatePath()
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, object>();
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var stateGroupKey = AddExtraUnderscoresToString(stateGroupWithQualifier);
                newRow[stateGroupKey] = "";
            }

            newRow["AudioFiles"] = new List<string> {};
            newRow["AudioFilesDisplay"] = string.Empty;

            DataGridData.Add(newRow);
        }

        public void RemoveStatePath(Dictionary<string, object> rowToRemove)
        {
            DataGridData.Remove(rowToRemove);
        }

        public static void AddAudioFiles(Dictionary<string, object> dataGridRow, TextBox textBox)
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = "WAV files (*.wav)|*.wav"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePaths = dialog.FileNames;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);
                var filePathsString = string.Join(", ", filePaths.Select(filePath => $"\"{filePath}\""));

                textBox.Text = fileNamesString;
                textBox.ToolTip = filePathsString;

                var audioFiles = (List<string>)dataGridRow["AudioFiles"];
                audioFiles.AddRange(filePaths);

                dataGridRow["AudioFilesDisplay"] = fileNamesString;
            }
        }

        public void CreateAudioProjectEventsListFromAudioProject()
        {
            AudioProjectEvents.Clear();

            foreach (var dialogueEventItem in AudioEditorInstance.AudioProject.DialogueEvents)
                AudioProjectEvents.Add(dialogueEventItem.Name);
        }


        public void ResetAudioEditorViewModelData()
        {
            SelectedAudioProjectEvent = null;
            ShowCustomStatesOnly = false;
            AudioProjectEvents.Clear();
            DataGridData.Clear();
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
