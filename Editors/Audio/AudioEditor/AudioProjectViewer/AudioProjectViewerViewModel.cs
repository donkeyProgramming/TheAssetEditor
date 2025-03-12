using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IEventHub _eventHub;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
        private readonly DataManager _dataManager;
        private readonly AudioProjectViewerDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;
        private readonly AudioProjectDataServiceFactory _audioProjectDataServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectViewerViewModel>();

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

        public AudioProjectViewerViewModel(
            IEventHub eventHub,
            IAudioRepository audioRepository,
            IAudioEditorService audioEditorService,
            DataManager dataManager,
            AudioProjectViewerDataGridServiceFactory audioProjectViewerDataGridServiceFactory,
            AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _eventHub = eventHub;
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _dataManager = dataManager;
            _audioProjectViewerDataGridServiceFactory = audioProjectViewerDataGridServiceFactory;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;

            AudioProjectViewerLabel = $"{DisplayName}";

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
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

        public void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
        {
            ResetAudioProjectViewerLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = nodeSelectedEvent.SelectedNode;
            if (selectedNode.NodeType == NodeType.ActionEventSoundBank)
            {
                SetAudioProjectViewerLabel(selectedNode.Name);

                var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded Action Event SoundBank: {selectedNode.Name}");
            }
            else if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                SetAudioProjectViewerLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));

                // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
                _audioEditorService.BuildModdedStatesByStateGroupLookup(_audioEditorService.AudioProject.StateGroups, _audioEditorService.ModdedStatesByStateGroupLookup);

                var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded Dialogue Event: {selectedNode.Name}");
            }
            else if (selectedNode.NodeType == NodeType.StateGroup)
            {
                SetAudioProjectViewerLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));

                var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded State Group: {selectedNode.Name}");
            }

            SetCopyEnablement();
            SetPasteEnablement();
        }

        public void ShowSettingsFromAudioProjectViewerItem()
        {
            var audioSettings = new AudioProjectData.AudioSettings();
            var audioFiles = new List<AudioFile>();

            var audioProjectItem = AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            if (audioProjectItem.NodeType == NodeType.ActionEventSoundBank)
            {
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromAudioProjectViewerActionEvent(AudioEditorViewModel, _audioEditorService);

                var selectedAudioProjectViewerDataGridRow = AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0];
                var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioProjectItem.Name);
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(selectedAudioProjectViewerDataGridRow, soundBank);

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
                audioSettings = AudioProjectHelpers.GetAudioSettingsFromAudioProjectViewerStatePath(AudioEditorViewModel, _audioEditorService, _audioRepository);

                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, AudioEditorViewModel.GetSelectedAudioProjectNodeName());
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(_audioRepository, AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0], dialogueEvent);

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
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, AudioEditorViewModel.GetSelectedAudioProjectNodeName());
                var dialogueEventStateGroups = _audioRepository
                    .QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name]
                    .Select(kvp => DataGridHelpers.AddExtraUnderscoresToString(kvp.Key))
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
            _dataManager.HandleEditingData(AudioEditorViewModel);
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            _dataManager.HandleRemovingData(AudioEditorViewModel);
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
                dialogueEventDataService.AddToAudioProject(AudioEditorViewModel);
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
            AudioProjectViewerLabel = $"Audio Project Editor {label}";
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
            DataGridHelpers.ClearDataGrid(AudioProjectViewerDataGrid);
        }

        public void Close() { }
    }
}
