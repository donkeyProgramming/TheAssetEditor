using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;
        private readonly DataManager _dataManager;
        private readonly AudioProjectDataServiceFactory _audioProjectDataServiceFactory;

        public string DisplayName { get; set; } = "Audio Project Viewer";

        [ObservableProperty] private string _audioProjectViewerLabel;
        [ObservableProperty] private string _audioProjectViewerDataGridTag = "AudioProjectViewerDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectViewerDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        public AudioProjectViewerViewModel(IAudioRepository audioRepository, IAudioProjectService audioProjectService, DataManager dataManager, AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
            _dataManager = dataManager;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;

            _audioProjectViewerLabel = $"{DisplayName}";
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            if (SelectedDataGridRows.Count == 0)
                SetSelectedDataGridRows(selectedItems);

            if (AudioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
            {
                ShowSettingsFromAudioProjectViewerItem();
                AudioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();
            }

            SetSelectedDataGridRows(selectedItems);
            SetButtonEnablement();
            SetCopyEnablement();
        }

        public void ShowSettingsFromAudioProjectViewerItem()
        {
            var audioSettings = new Data.AudioSettings();
            var audioFiles = new List<AudioFile>();

            var audioProjectItem = AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            if (audioProjectItem.NodeType == NodeType.ActionEventSoundBank)
            {
                audioSettings = DataHelpers.GetAudioSettingsFromAudioProjectViewerActionEventItem(AudioEditorViewModel, _audioProjectService);

                var selectedAudioProjectViewerDataGridRow = AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0];
                var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioProjectItem.Name);
                var actionEvent = DataHelpers.GetActionEventFromDataGridRow(selectedAudioProjectViewerDataGridRow, soundBank);

                if (actionEvent.Sound != null)
                {
                    audioFiles.Add(new AudioFile()
                    {
                        FileName = actionEvent.Sound.WavFileName,
                        FilePath = actionEvent.Sound.WavFilePath,
                    });
                }
                else
                {
                    foreach (var sound in actionEvent.RandomSequenceContainer.Sounds)
                    {
                        audioFiles.Add(new AudioFile()
                        {
                            FileName = sound.WavFileName,
                            FilePath = sound.WavFilePath,
                        });
                    }
                }
            }
            else if (audioProjectItem.NodeType == NodeType.DialogueEvent)
            {
                audioSettings = DataHelpers.GetAudioSettingsFromAudioProjectViewerStatePathItem(AudioEditorViewModel, _audioProjectService, _audioRepository);

                var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, AudioEditorViewModel.GetSelectedAudioProjectNodeName());
                var statePath = DataHelpers.GetStatePathFromDataGridRow(_audioRepository, AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0], dialogueEvent);

                if (statePath.Sound != null)
                {
                    audioFiles.Add(new AudioFile()
                    {
                        FileName = statePath.Sound.WavFileName,
                        FilePath = statePath.Sound.WavFilePath,
                    });
                }
                else
                {
                    foreach (var sound in statePath.RandomSequenceContainer.Sounds)
                    {
                        audioFiles.Add(new AudioFile()
                        {
                            FileName = sound.WavFileName,
                            FilePath = sound.WavFilePath,
                        });
                    }
                }

            }

            AudioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsFromAudioProjectItemAudioSettings(audioSettings, audioFiles.Count);
            AudioEditorViewModel.AudioSettingsViewModel.SetAudioFiles(audioFiles);
        }

        private void SetSelectedDataGridRows(IList selectedItems)
        {
            SelectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, string>>())
                SelectedDataGridRows.Add(item);
        }

        public void SetCopyEnablement()
        {
            if (AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows != null)
                AudioEditorViewModel.AudioProjectViewerViewModel.IsCopyEnabled = AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.Any();
        }

        partial void OnAudioProjectViewerDataGridChanged(ObservableCollection<Dictionary<string, string>> value)
        {
            if (AudioProjectViewerDataGrid != null)
            {
                AudioProjectViewerDataGrid.CollectionChanged += AudioProjectViewerDataGrid_CollectionChanged;
                OnAudioProjectViewerDataGridChanged();
            }
        }

        private void AudioProjectViewerDataGrid_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnAudioProjectViewerDataGridChanged();
        }

        private void OnAudioProjectViewerDataGridChanged()
        {
            if (AudioProjectViewerDataGrid != null && AudioProjectViewerDataGrid.Count > 0)
                SetPasteEnablement();
        }

        public void SetPasteEnablement()
        {
            if (!AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Any())
            {
                AudioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows
                .Any(copiedRow => AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid
                .Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, AudioEditorViewModel.GetSelectedAudioProjectNodeName());
                var dialogueEventStateGroups = _audioRepository
                    .QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name]
                    .Select(kvp => DataHelpers.AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedDataGridRowStateGroups = AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows[0]
                    .Select(kvp => kvp.Key)
                    .ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                AudioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }

        [RelayCommand] public void EditAudioProjectViewerDataGridRow()
        {
            _dataManager.HandleEditingAudioProjectViewerData(AudioEditorViewModel);
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            _dataManager.HandleRemovingAudioProjectViewerData(AudioEditorViewModel);
        }

        [RelayCommand] public void CopyRows()
        {
            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
                CopyDialogueEventRows();
        }

        public void CopyDialogueEventRows()
        {
            AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows = [];

            foreach (var item in AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows)
                AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Add(new Dictionary<string, string>(item));

            SetPasteEnablement();
        }

        [RelayCommand]  public void PasteRows()
        {
            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
                PasteDialogueEventRows();
        }

        public void PasteDialogueEventRows()
        {
            foreach (var copiedDataGridRow in AudioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows)
            {
                AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(copiedDataGridRow);

                var dialogueEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.DialogueEvent);
                dialogueEventDataService.AddAudioProjectEditorDataGridDataToAudioProject(AudioEditorViewModel);
            }

            SetPasteEnablement();
        }

        public void SetButtonEnablement()
        {
            ResetButtonEnablement();

            if (SelectedDataGridRows.Count == 1)
            {
                IsUpdateRowButtonEnabled = true;
                IsRemoveRowButtonEnabled = true;
            }
            else if (SelectedDataGridRows.Count > 1)
                IsRemoveRowButtonEnabled = true;
        }

        public void SetAudioProjectViewerLabel(string label)
        {
            AudioProjectViewerLabel = label;
        }

        public void ResetAudioProjectViewerLabel()
        {
            AudioProjectViewerLabel = $"Audio Project Viewer";
        }
        
        public void ResetButtonEnablement()
        {
            IsUpdateRowButtonEnabled = false;
            IsRemoveRowButtonEnabled = false;
        }

        public void ResetDataGrid()
        {
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectViewerDataGridTag));
            DataGridHelpers.ClearDataGridCollection(AudioProjectViewerDataGrid);
        }

        public void Close() { }
    }
}
