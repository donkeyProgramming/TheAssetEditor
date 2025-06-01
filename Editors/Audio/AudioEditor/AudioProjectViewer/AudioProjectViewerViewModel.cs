using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject
    {
        private readonly IAudioProjectUICommandFactory _audioProjectUICommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
        private readonly AudioProjectViewerDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectViewerViewModel>();

        [ObservableProperty] private string _audioProjectViewerLabel;

        [ObservableProperty] private string _audioProjectViewerDataGridTag = "AudioProjectViewerDataGrid";
        [ObservableProperty] private DataTable _audioProjectViewerDataGrid;

        public List<DataRow> _selectedDataGridRows;
        public List<Dictionary<string, object>> _copiedRows = [];

        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        public AudioProjectViewerViewModel(
            IAudioProjectUICommandFactory audioProjectUICommandFactory,
            IEventHub eventHub,
            IAudioRepository audioRepository,
            IAudioEditorService audioEditorService,
            AudioProjectViewerDataGridServiceFactory audioProjectViewerDataGridServiceFactory)
        {
            _audioProjectUICommandFactory = audioProjectUICommandFactory;
            _eventHub = eventHub;
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _audioProjectViewerDataGridServiceFactory = audioProjectViewerDataGridServiceFactory;

            AudioProjectViewerLabel = $"Audio Project Viewer";

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _eventHub.Register<ItemAddedEvent>(this, OnItemAdded);

            _eventHub.Register<RemoveRowEvent>(this, OnRowRemoved);
        }

        public void OnRowRemoved(RemoveRowEvent removeRowEvent)
        {
            var row = removeRowEvent.row;
            AudioProjectViewerDataGrid.Rows.Remove(row);
        }

        public void OnDataGridSelectionChanged(List<DataRow> selectedItems)
        {
            if (_selectedDataGridRows.Count == 0)
                SetSelectedDataGridRows(selectedItems);

            // TODO: Should probably add an event for this.
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

            var selectedNode = _audioEditorService.SelectedExplorerNode;
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
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.InsertDataGridRow();

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        private void SetSelectedDataGridRows(List<DataRow> selectedItems)
        {
            _selectedDataGridRows = selectedItems;
        }

        public void SetCopyEnablement()
        {
            if (_selectedDataGridRows != null)
                IsCopyEnabled = _selectedDataGridRows.AsEnumerable().Any();
        }

        partial void OnAudioProjectViewerDataGridChanged(DataTable value)
        {
            if (AudioProjectViewerDataGrid != null && AudioProjectViewerDataGrid.Rows.Count > 0)
                SetPasteEnablement();
        }

        public void SetPasteEnablement()
        {
            if (_copiedRows.Count == 0)
            {
                IsPasteEnabled = false;
                return;
            }

            var viewerTable = _audioEditorService.GetViewerDataGrid();
            var viewerColumns = viewerTable.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToList();

            var schemaMatches = viewerColumns.Count == _copiedRows[0].Count && viewerColumns.All(column => _copiedRows[0].ContainsKey(column));
            if (!schemaMatches)
            {
                IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = _copiedRows
                .Any(copied => viewerTable.AsEnumerable()
                    .Any(viewer => viewerColumns.All(col => Equals(copied[col], viewer[col]))));

            var selectedNode = _audioEditorService.SelectedExplorerNode;

            if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, selectedNode.Name);

                var dialogueEventStateGroups = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name]
                    .Select(kvp => DataGridHelpers.AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedStateGroups = _copiedRows[0].Keys.ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedStateGroups);

                IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }

        [RelayCommand] public void EditAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            _eventHub.Publish(new ItemEditedEvent()); // Publish before removing to ensure that an item is still selected

            RemoveData(selectedNode.NodeType);

            _logger.Here().Information($"Edited {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            RemoveData(selectedNode.NodeType);
        }

        private void RemoveData(NodeType nodeType)
        {
            var selectedRows = _audioEditorService.GetSelectedViewerRows();
            foreach (var row in selectedRows)
                _audioProjectUICommandFactory.Create(AudioProjectCommandAction.RemoveFromAudioProject, nodeType).Execute(row);

            _eventHub.Publish(new SetEnablementEvent()); // Publish after removing to ensure that the enablement uses the update data
        }

        [RelayCommand] public void CopyRows()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType == NodeType.DialogueEvent)
                CopyDialogueEventRows();
        }

        public void CopyDialogueEventRows()
        {
            var table = _selectedDataGridRows[0].Table;

            _copiedRows = _selectedDataGridRows
                .Select(row =>
                    table.Columns.Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => row[col]))
                .ToList();

            SetPasteEnablement();
        }

        [RelayCommand]  public void PasteRows()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType == NodeType.DialogueEvent)
                PasteDialogueEventRows();
        }

        public void PasteDialogueEventRows()
        {
            foreach (var copiedRow in _copiedRows)           
            {
                var pastedRow = AudioProjectViewerDataGrid.NewRow();
                foreach (var copiedRowData in copiedRow)
                    pastedRow[copiedRowData.Key] = copiedRowData.Value;

                AudioProjectViewerDataGrid.Rows.Add(pastedRow);

                _audioProjectUICommandFactory.Create(AudioProjectCommandAction.AddToAudioProject, NodeType.DialogueEvent).Execute(pastedRow);
            }

            SetPasteEnablement();
        }

        public void SetButtonEnablement()
        {
            ResetButtonEnablement();

            if (_selectedDataGridRows.Count == 1)
            {
                IsUpdateRowButtonEnabled = true;
                IsRemoveRowButtonEnabled = true;
            }
            else if (_selectedDataGridRows.Count > 1)
                IsRemoveRowButtonEnabled = true;
        }

        public void SetAudioProjectViewerLabel(string label)
        {
            AudioProjectViewerLabel = $"Audio Project Viewer {label}";
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
            AudioProjectViewerDataGrid = new DataTable();
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectViewerDataGridTag));
        }
    }
}
