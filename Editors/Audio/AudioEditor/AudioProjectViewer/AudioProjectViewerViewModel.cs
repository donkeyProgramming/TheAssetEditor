using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject
    {
        private readonly IAudioProjectUICommandFactory _audioProjectUICommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectViewerViewModel>();

        [ObservableProperty] private string _audioProjectViewerLabel;

        [ObservableProperty] private string _audioProjectViewerDataGridTag;
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
            IDataGridServiceFactory audioEditorDataGridServiceFactory)
        {
            _audioProjectUICommandFactory = audioProjectUICommandFactory;
            _eventHub = eventHub;
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _audioProjectViewerDataGridServiceFactory = audioEditorDataGridServiceFactory;

            AudioProjectViewerLabel = $"Audio Project Viewer";
            AudioProjectViewerDataGridTag = _audioEditorService.AudioProjectViewerDataGridTag;

            _eventHub.Register<AddViewerTableColumnEvent>(this, AddTableColumn);
            _eventHub.Register<AddViewerTableRowEvent>(this, AddTableRow);
            _eventHub.Register<RemoveViewerTableRowEvent>(this, RemoveTableRow);
            _eventHub.Register<SetDataGridContextMenuEvent>(this, SetDataGridContextMenu);





            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
        }

        public void AddTableColumn(AddViewerTableColumnEvent addTableColumnEvent)
        {
            var column = addTableColumnEvent.Column;
            if (!AudioProjectViewerDataGrid.Columns.Contains(column.ColumnName))
            {
                AudioProjectViewerDataGrid.Columns.Add(column);
                _logger.Here().Information($"Added {_audioEditorService.SelectedExplorerNode.NodeType} column to Audio Project Viewer table for {_audioEditorService.SelectedExplorerNode.Name} ");
            }
        }

        public void AddTableRow(AddViewerTableRowEvent addViewerTableRowEvent)
        {
            DataTableHelpers.InsertRowAlphabetically(AudioProjectViewerDataGrid, addViewerTableRowEvent.Row);
            _logger.Here().Information($"Added {_audioEditorService.SelectedExplorerNode.NodeType} row to Audio Project Viewer table for {_audioEditorService.SelectedExplorerNode.Name}");
        }

        public void RemoveTableRow(RemoveViewerTableRowEvent removeViewerTableRowEvent)
        {
            AudioProjectViewerDataGrid.Rows.Remove(removeViewerTableRowEvent.Row);
            _logger.Here().Information($"Removed {_audioEditorService.SelectedExplorerNode.NodeType} row from Audio Project Viewer table for {_audioEditorService.SelectedExplorerNode.Name}");
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            RemoveDataFromAudioProject(selectedNode.NodeType);
        }

        private void RemoveDataFromAudioProject(NodeType nodeType)
        {
            var selectedRows = _audioEditorService.GetSelectedViewerRows();
            foreach (var row in selectedRows)
                _audioProjectUICommandFactory.Create(AudioProjectCommandAction.RemoveFromAudioProject, nodeType).Execute(row);

            _eventHub.Publish(new SetEnablementEvent()); // Publish after removing to ensure that the enablement uses the update data
        }

        [RelayCommand] public void EditAudioProjectViewerDataGridRow()
        {
            _eventHub.Publish(new EditViewerRowEvent(_selectedDataGridRows[0])); // Publish before removing to ensure that an item is still selected

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            RemoveDataFromAudioProject(selectedNode.NodeType);

            _logger.Here().Information($"Editing {selectedNode.NodeType} row in Audio Project Viewer table for {selectedNode.Name}");
        }

        public void SetDataGridContextMenu(SetDataGridContextMenuEvent setDataGridContextMenuEvent)
        {
            var contextMenu = new ContextMenu();

            var copyMenuItem = new MenuItem
            {
                Header = "Copy",
                Command = CopyRowsCommand
            };

            BindingOperations.SetBinding(copyMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsCopyEnabled")
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                });

            var pasteMenuItem = new MenuItem
            {
                Header = "Paste",
                Command = PasteRowsCommand
            };

            BindingOperations.SetBinding(pasteMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsPasteEnabled")
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                });

            contextMenu.Items.Add(copyMenuItem);
            contextMenu.Items.Add(pasteMenuItem);

            setDataGridContextMenuEvent.DataGrid.ContextMenu = contextMenu;
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
            var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(AudioProjectDataGrid.Viewer, selectedNodeType);
            dataGridService.LoadDataGrid(AudioProjectViewerDataGrid);
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
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridFromTag(AudioProjectViewerDataGridTag));
        }
    }
}
