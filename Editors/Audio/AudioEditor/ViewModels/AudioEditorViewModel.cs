using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        partial void OnSelectedAudioProjectEventChanged(string value)
        {
            AudioEditorInstance.SelectedAudioProjectEvent = value;

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
            var window = _windowFactory.Create<AudioEditorNewAudioProjectViewModel, AudioEditorNewAudioProjectView>("New Audio Project", 560, 500);
            window.AlwaysOnTop = false;
            window.ResizeMode = ResizeMode.NoResize;

            // Set the close action
            if (window.DataContext is AudioEditorNewAudioProjectViewModel viewModel)
                viewModel.SetCloseAction(() => window.Close());

            window.ShowWindow();
        }

        /*
        [RelayCommand] public void LoadAudioProject()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".audioproject"]);

            if (browser.ShowDialog())
            {
                // Remove any pre-existing data otherwise DataGrid isn't happy.
                AudioEditorInstance.ResetAudioEditorData();
                ResetAudioEditorViewModelData();

                // Create the object for State Groups with qualifiers so that their keys in the AudioProject dictionary are unique.
                AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

                var filePath = _packFileService.GetFullPath(browser.SelectedFile);
                var file = _packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var audioProjectData = ConvertFromVOAudioProject(audioProjectJson);

                AudioEditorInstance.AudioProject = audioProjectData;

                var audioProjectFileName = AudioEditorInstance.AudioProject.Settings.AudioProjectName;
                _logger.Here().Information($"Loaded Audio Project file: {audioProjectFileName}");

                // Create the list of Events used in the Events ComboBox.
                CreateAudioProjectEventsListFromAudioProject();

                // Load the object which stores the custom States for use in the States ComboBox.
                //PrepareCustomStatesForComboBox(this);
            }
        }
        */

        [RelayCommand] public void SaveAudioProject()
        {
            UpdateAudioProjectWithDataGridData();

            AddAudioProjectToPackFile(_packFileService);
        }

        public void LoadDialogueEvent(IAudioRepository audioRepository, bool showCustomStatesOnly)
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            ConfigureDataGrid(this, audioRepository, showCustomStatesOnly);

            var dialogueEvent = AudioEditorInstance.AudioProject.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.DialogueEventName == SelectedAudioProjectEvent);
            var decisionTree = dialogueEvent.DecisionTree;

            if (dialogueEvent != null && decisionTree.Count > 0)
            {
                // Clear previous data if needed
                DataGridData.Clear();

                // Process Decision Tree and populate the objects in dataGridData. Make sure to add the kvp for the column header i.e. the stateGroupWithQualifier (with extra underscores) to prevent a binding error. 
                foreach (var statePath in decisionTree)
                {
                    var audioFiles = statePath.AudioFiles;
                    var fileNamesString = string.Join(", ", audioFiles);

                    var dataGridRow = new Dictionary<string, object>
                    {
                        ["AudioFiles"] = new List<string>(statePath.AudioFiles), // Create a new instance of the list to avoid referencing the original collection.
                        ["AudioFilesDisplay"] = fileNamesString
                    };

                    if (DialogueEventsWithStateGroupsWithQualifiers.TryGetValue(SelectedAudioProjectEvent, out var stateGroupsWithQualifiers))
                    {
                        foreach (var (stateGroup, stateGroupWithQualifier) in stateGroupsWithQualifiers)
                            dataGridRow[AddExtraUnderScoresToString(stateGroupWithQualifier)] = string.Empty;
                    }

                    DataGridData.Add(dataGridRow);
                }
            }

            _logger.Here().Information($"Loaded event: {SelectedAudioProjectEvent}");
        }

        [RelayCommand] public void AddStatePath()
        {
            if (string.IsNullOrEmpty(SelectedAudioProjectEvent))
                return;

            var newRow = new Dictionary<string, object>();
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];

            foreach (var (stateGroup, stateGroupWithQualifier) in stateGroupsWithQualifiers)
            {
                var stateGroupKey = AddExtraUnderScoresToString(stateGroupWithQualifier);
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

        public void UpdateAudioProjectWithDataGridData()
        {
            if (DataGridData == null || SelectedAudioProjectEvent == null)
                return;

            var audioProject = AudioEditorInstance.AudioProject;
            var dialogueEvent = audioProject.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.DialogueEventName == SelectedAudioProjectEvent); // Find the corresponding DialogueEvent in AudioProject
            var decisionTree = dialogueEvent.DecisionTree;
            decisionTree.Clear();

            foreach (var dataGridItem in DataGridData)
            {
                // Validation to ensure that the State Groups are in the correct order.
                var orderedStateGroupsAndStates = ValidateStateGroupsOrder(dataGridItem);

                // Generate the StatePath object.
                var statePath = new StatePath
                {
                    Path = string.Join(".", orderedStateGroupsAndStates.Values),
                    AudioFiles = dataGridItem.ContainsKey("AudioFiles") ? dataGridItem["AudioFiles"] as List<string> : new List<string>(),
                };

                dialogueEvent.DecisionTree.Add(statePath);
            }
        }

        private Dictionary<string, string> ValidateStateGroupsOrder(Dictionary<string, object> dataGridItem)
        {
            var stateGroupsAndStates = new Dictionary<string, string>();
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[SelectedAudioProjectEvent];
            var orderedStateGroupsAndStates = new Dictionary<string, string>();

            foreach (var kvp in dataGridItem)
            {
                var key = kvp.Key;
                var value = kvp.Value.ToString();

                if (key != "AudioFiles" && key != "AudioFilesDisplay" && key != "Path") // access only the State Group data items as they contain the States data.
                {
                    var stateGroupWithQualifier = key;
                    var state = value;
                    stateGroupsAndStates[stateGroupWithQualifier] = state;
                }
            }

            foreach (var (stateGroup, stateGroupWithQualifier) in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifierWithExtraUnderscores = AddExtraUnderScoresToString(stateGroupWithQualifier);

                if (stateGroupsAndStates.ContainsKey(stateGroupWithQualifierWithExtraUnderscores))
                    orderedStateGroupsAndStates[stateGroupWithQualifierWithExtraUnderscores] = stateGroupsAndStates[stateGroupWithQualifierWithExtraUnderscores];
            }

            return orderedStateGroupsAndStates;
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
                AudioProjectEvents.Add(dialogueEventItem.DialogueEventName);
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
