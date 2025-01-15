using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioSettingsEditor;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataManager;
using static Editors.Audio.Utility.SoundPlayer;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Project Editor";

        [ObservableProperty] private string _audioProjectEditorLabel = "Audio Project Editor";
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorSingleRowDataGrid;

        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isAddAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;

        public AudioProjectEditorViewModel (AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                // Clear the previous DataGrid Data
                DataGridHelpers.ClearDataGridCollection(AudioProjectEditorSingleRowDataGrid);

                var parameters = new AudioProjectDataServiceParameters();
                parameters.AudioEditorViewModel = _audioEditorViewModel;
                parameters.AudioProjectService = _audioProjectService;
                parameters.AudioRepository = _audioRepository;
                parameters.DialogueEvent = selectedDialogueEvent;

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorSingleRowDataGridToFullDataGrid()
        {
            if (AudioProjectEditorSingleRowDataGrid.Count == 0)
                return;

            HandleAddingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void UpdateAudioProjectEditorFullDataGridRow()
        {
            HandleUpdatingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void RemoveAudioProjectEditorFullDataGridRow()
        {
            HandleRemovingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        [RelayCommand]  public void AddSelectedAudioFiles()
        {
            var dataGridRow = AudioProjectEditorSingleRowDataGrid[0];

            var selectedWavFilePaths = _audioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes
                .Select(wavFile => wavFile.FilePath)
                .ToList();

            var selectedWavFileNames = _audioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes
                .Select(wavFile => wavFile.Name)
                .ToList();

            var fileNamesString = string.Join(", ", selectedWavFileNames);
            var filePathsString = string.Join(", ", selectedWavFilePaths.Select(filePath => $"\"{filePath}\""));

            var audioFiles = new List<string>(selectedWavFilePaths);
            dataGridRow["AudioFiles"] = audioFiles;
            dataGridRow["AudioFilesDisplay"] = fileNamesString;

            var dataGrid = DataGridHelpers.GetDataGridByTag(AudioProjectEditorDataGridTag);
            var textBox = DataGridHelpers.FindVisualChild<TextBox>(dataGrid, "AudioFilesDisplay");
            if (textBox != null)
            {
                textBox.Text = fileNamesString;
                textBox.ToolTip = filePathsString;
            }

            if (audioFiles.Count > 1)
                _audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = true;
            else
                _audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = false;

            AudioSettingsEditorViewModel.SetAudioSettingsEnablement(_audioEditorViewModel.AudioSettingsViewModel);
            SetIsAddRowButtonEnabled(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        // Maybe change this to play a file in the explorer
        [RelayCommand] public void PlayRandomAudioFile()
        {
            if (_audioEditorViewModel.SelectedDataGridRows[0].TryGetValue("AudioFiles", out var audioFilesObj) && audioFilesObj is List<string> audioFiles && audioFiles.Count != 0)
            {
                var random = new Random();
                var randomIndex = random.Next(audioFiles.Count);
                var randomAudioFile = audioFiles[randomIndex];
                PlayWavFile(randomAudioFile);
            }
        }

        public void Close() {}
    }
}
