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
        private readonly IEventHub _eventHub;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
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
            AudioProjectViewerDataGridServiceFactory audioProjectViewerDataGridServiceFactory,
            AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _eventHub = eventHub;
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _audioProjectViewerDataGridServiceFactory = audioProjectViewerDataGridServiceFactory;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;

            AudioProjectViewerLabel = $"{DisplayName}";

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _eventHub.Register<ItemAddedEvent>(this, OnItemAdded);
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            if (SelectedDataGridRows.Count == 0)
                SetSelectedDataGridRows(selectedItems);

            // TODO: Should really add an event for this.
            if (_audioEditorService.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
            {
                _audioEditorService.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewerItem();
                _audioEditorService.AudioSettingsViewModel.DisableAllAudioSettings();
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

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.ActionEventSoundBank)
            {
                SetAudioProjectViewerLabel(selectedNode.Name);
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
                _audioEditorService.BuildModdedStatesByStateGroupLookup(_audioEditorService.AudioProject.StateGroups, _audioEditorService.ModdedStatesByStateGroupLookup);

                SetAudioProjectViewerLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.NodeType == NodeType.StateGroup)
            {
                SetAudioProjectViewerLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedNode.NodeType}: {selectedNode.Name}");

            SetCopyEnablement();
            SetPasteEnablement();
        }

        private void LoadDataGrid(NodeType selectedNodeType)
        {
            var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNodeType);
            dataGridService.LoadDataGrid();
        }

        public void OnItemAdded(ItemAddedEvent itemAddedEvent)
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.InsertDataGridRow();

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        private void SetSelectedDataGridRows(IList selectedItems)
        {
            SelectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, string>>())
                SelectedDataGridRows.Add(item);
        }

        public void SetCopyEnablement()
        {
            if (SelectedDataGridRows != null)
                IsCopyEnabled = SelectedDataGridRows.Any();
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
            if (!CopiedDataGridRows.Any())
            {
                IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = CopiedDataGridRows
                .Any(copiedRow => _audioEditorService.GetViewerDataGrid()
                .Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
                var dialogueEventStateGroups = _audioRepository
                    .QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name]
                    .Select(kvp => DataGridHelpers.AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedDataGridRowStateGroups = CopiedDataGridRows[0]
                    .Select(kvp => kvp.Key)
                    .ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }

        [RelayCommand] public void EditAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            _eventHub.Publish(new ItemEditedEvent()); // Publish before removing to ensure that an item is still selected

            RemoveData(selectedNode.NodeType);

            _logger.Here().Information($"Edited {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            RemoveData(selectedNode.NodeType);
        }

        private void RemoveData(NodeType nodeType)
        {
            var actionEventDataService = _audioProjectDataServiceFactory.GetService(nodeType);
            actionEventDataService.RemoveFromAudioProject();
        }

        [RelayCommand] public void CopyRows()
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.DialogueEvent)
                CopyDialogueEventRows();
        }

        public void CopyDialogueEventRows()
        {
            CopiedDataGridRows = [];

            foreach (var item in SelectedDataGridRows)
                CopiedDataGridRows.Add(new Dictionary<string, string>(item));

            SetPasteEnablement();
        }

        [RelayCommand]  public void PasteRows()
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.DialogueEvent)
                PasteDialogueEventRows();
        }

        public void PasteDialogueEventRows()
        {
            foreach (var copiedDataGridRow in CopiedDataGridRows)
            {
                _audioEditorService.GetViewerDataGrid().Add(copiedDataGridRow);

                var dialogueEventDataService = _audioProjectDataServiceFactory.GetService(NodeType.DialogueEvent);
                dialogueEventDataService.AddToAudioProject();
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
