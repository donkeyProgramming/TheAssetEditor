using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioSettingsEditor;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.CopyPasteHandler;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataManager;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.Utility.SoundPlayer;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioSettingsEditorViewModel AudioSettingsViewModel { get; set; }


        public string DisplayName { get; set; } = "Audio Editor";

        // Audio Editor data
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorSingleRowDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorFullDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;

        // Audio Project Editor
        [ObservableProperty] private string _audioProjectEditorLabel = "Audio Project Editor";
        [ObservableProperty] private string _audioProjectViewerLabel = "Audio Project Viewer";
        [ObservableProperty] private string _audioProjectEditorSingleRowDataGridTag = "AudioProjectEditorSingleRowDataGrid";
        [ObservableProperty] private string _audioProjectEditorFullDataGridTag = "AudioProjectEditorFullDataGrid";
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isAddAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        public AudioEditorViewModel(IAudioRepository audioRepository, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            AudioProjectExplorerViewModel = new AudioProjectExplorerViewModel(this, _audioRepository, _audioProjectService);
            AudioFilesExplorerViewModel = new AudioFilesExplorerViewModel(this, _packFileService);
            AudioSettingsViewModel = new AudioSettingsEditorViewModel();


            Initialise();

            CheckAudioEditorDialogueEventIntegrity(_audioRepository, DialogueEventData);
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            SetButtonEnablement(this, _audioProjectService, selectedItems);
            SetIsCopyEnabled(this);
        }

        partial void OnAudioProjectEditorFullDataGridChanged(ObservableCollection<Dictionary<string, object>> value)
        {
            if (AudioProjectEditorFullDataGrid != null)
            {
                AudioProjectEditorFullDataGrid.CollectionChanged += AudioProjectEditorFullDataGrid_CollectionChanged;
                OnAudioProjectEditorFullDataGridChanged();
            }
        }

        private void AudioProjectEditorFullDataGrid_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnAudioProjectEditorFullDataGridChanged();
        }

        private void OnAudioProjectEditorFullDataGridChanged()
        {
            if (AudioProjectEditorFullDataGrid != null && AudioProjectEditorFullDataGrid.Count > 0)
                SetIsPasteEnabled(this, _audioRepository, _audioProjectService);
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                // Clear the previous DataGrid Data
                DataGridHelpers.ClearDataGridCollection(AudioProjectEditorSingleRowDataGrid);

                var parameters = new AudioProjectDataServiceParameters();
                parameters.AudioEditorViewModel = this;
                parameters.AudioProjectService = _audioProjectService;
                parameters.AudioRepository = _audioRepository;
                parameters.DialogueEvent = selectedDialogueEvent;

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            }
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_packFileService, this, _audioProjectService, _packFileUiProvider);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject(_packFileService);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(_packFileService, _audioRepository, this, _packFileUiProvider);
        }

        [RelayCommand] public void UpdateAudioProjectEditorFullDataGridRow()
        {
            HandleUpdatingRowData(this, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void RemoveAudioProjectEditorFullDataGridRow()
        {
            HandleRemovingRowData(this, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void AddSelectedAudioFiles()
        {
            var dataGridRow = AudioProjectEditorSingleRowDataGrid[0];

            var selectedWavFilePaths = AudioFilesExplorerViewModel.SelectedTreeNodes
                .Select(wavFile => wavFile.FilePath)
                .ToList();

            var selectedWavFileNames = AudioFilesExplorerViewModel.SelectedTreeNodes
                .Select(wavFile => wavFile.Name)
                .ToList();

            var fileNamesString = string.Join(", ", selectedWavFileNames);
            var filePathsString = string.Join(", ", selectedWavFilePaths.Select(filePath => $"\"{filePath}\""));

            var audioFiles = new List<string>(selectedWavFilePaths);
            dataGridRow["AudioFiles"] = audioFiles;
            dataGridRow["AudioFilesDisplay"] = fileNamesString;

            var dataGrid = DataGridHelpers.GetDataGridByTag(AudioProjectEditorSingleRowDataGridTag);
            var textBox = DataGridHelpers.FindVisualChild<TextBox>(dataGrid, "AudioFilesDisplay");
            if (textBox != null)
            {
                textBox.Text = fileNamesString;
                textBox.ToolTip = filePathsString;
            }

            if (audioFiles.Count > 1)
                AudioSettingsViewModel.IsUsingMultipleAudioFiles = true;
            else
                AudioSettingsViewModel.IsUsingMultipleAudioFiles = false;

            AudioSettingsEditorViewModel.SetAudioSettingsEnablement(AudioSettingsViewModel);
            SetIsAddRowButtonEnabled(this, _audioProjectService, _audioRepository);
        }

        // Maybe change this to play a file in the explorer
        [RelayCommand] public void PlayRandomAudioFile()
        {
            if (SelectedDataGridRows[0].TryGetValue("AudioFiles", out var audioFilesObj) && audioFilesObj is List<string> audioFiles && audioFiles.Any())
            {
                var random = new Random();
                var randomIndex = random.Next(audioFiles.Count);
                var randomAudioFile = audioFiles[randomIndex];
                PlayWavFile(randomAudioFile);
            }
        }

        [RelayCommand] public void CopyRows()
        {
            if (AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent)
                CopyDialogueEventRows(this, _audioRepository, _audioProjectService);
        }

        [RelayCommand] public void PasteRows()
        {
            if (IsPasteEnabled && AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                PasteDialogueEventRows(this, _audioRepository, _audioProjectService, selectedDialogueEvent);
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorSingleRowDataGridToFullDataGrid()
        {
            if (AudioProjectEditorSingleRowDataGrid.Count == 0)
                return;

            HandleAddingRowData(this, _audioProjectService, _audioRepository);
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorSingleRowDataGrid = null;
            AudioProjectEditorFullDataGrid = null;
            SelectedDataGridRows = null;
            CopiedDataGridRows = null;
            AudioProjectExplorerViewModel._selectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel._previousSelectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel.AudioProjectTreeViewItems.Clear();
            AudioProjectExplorerViewModel.DialogueEventSoundBankFiltering.Clear();
        }

        public void Initialise()
        {
            AudioProjectEditorSingleRowDataGrid = [];
            AudioProjectEditorFullDataGrid = [];
            SelectedDataGridRows = [];
            CopiedDataGridRows = [];
            AudioProjectExplorerViewModel.DialogueEventPresets = [];
            AudioProjectExplorerViewModel.AudioProjectTreeViewItems = _audioProjectService.AudioProject.AudioProjectTreeViewItems;
        }

        public void Close()
        {
            ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
        }
    }
}
