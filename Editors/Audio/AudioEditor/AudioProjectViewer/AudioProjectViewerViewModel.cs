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
using Editors.Audio.AudioEditor.Models;
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
        private readonly IDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectViewerViewModel>();

        [ObservableProperty] private string _audioProjectViewerLabel;
        [ObservableProperty] private string _audioProjectViewerDataGridTag;
        [ObservableProperty] private DataTable _audioProjectViewerDataGrid;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        [ObservableProperty] public List<DataRow> _selectedRows;
        private List<DataRow> _copiedRows = [];

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

            _eventHub.Register<InitialiseViewModelDataEvent>(this, InitialiseData);
            _eventHub.Register<ResetViewModelDataEvent>(this, ResetData);
            _eventHub.Register<AddViewerTableColumnEvent>(this, AddTableColumn);
            _eventHub.Register<AddViewerTableRowEvent>(this, AddTableRow);
            _eventHub.Register<RemoveViewerTableRowEvent>(this, RemoveTableRow);
            _eventHub.Register<SetDataGridContextMenuEvent>(this, SetDataGridContextMenu);
            _eventHub.Register<ExplorerNodeSelectedEvent>(this, OnSelectedExplorerNodeChanged);
            _eventHub.Register<CopyRowsEvent>(this, CopyRows);
            _eventHub.Register<PasteRowsEvent>(this, PasteRows);
        }

        public void InitialiseData(InitialiseViewModelDataEvent e)
        {
            AudioProjectViewerDataGrid = new(); 
            SelectedRows = [];
            _copiedRows = [];

            AudioProjectViewerDataGrid = null;
        }

        public void ResetData(ResetViewModelDataEvent e)
        {
            AudioProjectViewerDataGrid = null;
            SelectedRows = null;
            _copiedRows = null;
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
            DataGridHelpers.InsertRowAlphabetically(AudioProjectViewerDataGrid, addViewerTableRowEvent.Row);
            _logger.Here().Information($"Added {_audioEditorService.SelectedExplorerNode.NodeType} row to Audio Project Viewer table for {_audioEditorService.SelectedExplorerNode.Name}");
        }

        public void RemoveTableRow(RemoveViewerTableRowEvent removeViewerTableRowEvent)
        {
            AudioProjectViewerDataGrid.Rows.Remove(removeViewerTableRowEvent.Row);
            _logger.Here().Information($"Removed {_audioEditorService.SelectedExplorerNode.NodeType} row from Audio Project Viewer table for {_audioEditorService.SelectedExplorerNode.Name}");
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

        public void OnSelectedExplorerNodeChanged(ExplorerNodeSelectedEvent explorerNodeSelectedEvent)
        {
            ResetAudioProjectViewerLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = explorerNodeSelectedEvent.TreeNode;
            if (selectedNode.IsActionEventSoundBank())
            {
                SetAudioProjectViewerLabel(selectedNode.Name);
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.IsDialogueEvent())
            {
                SetAudioProjectViewerLabel(DataGridHelpers.DuplicateUnderscores(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.IsStateGroup())
            {
                SetAudioProjectViewerLabel(DataGridHelpers.DuplicateUnderscores(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedNode.NodeType}: {selectedNode.Name}");

            SetCopyEnablement();
            SetPasteEnablement();
        }

        public void CopyRows(CopyRowsEvent e) => CopyRows();

        public void PasteRows(PasteRowsEvent e) => PasteRows();

        partial void OnSelectedRowsChanged(List<DataRow> value)
        {
            _audioEditorService.SelectedViewerRows = SelectedRows;

            _eventHub.Publish(new ViewerRowSelectionChangedEvent());

            SetButtonEnablement();
            SetCopyEnablement();
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            var selectedViewerRows = _audioEditorService.SelectedViewerRows;
            foreach (var row in selectedViewerRows)
                _audioProjectUICommandFactory.Create(AudioProjectCommandAction.RemoveFromAudioProject, selectedNode.NodeType).Execute(row);

            _eventHub.Publish(new SetEditorAddRowButtonEnablementEvent()); // Publish after removing to ensure that the enablement uses the update data
        }

        [RelayCommand] public void EditAudioProjectViewerDataGridRow()
        {
            _eventHub.Publish(new EditViewerRowEvent(SelectedRows[0])); // Publish before removing to ensure that an item is still selected

            RemoveAudioProjectViewerDataGridRow();

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            _logger.Here().Information($"Editing {selectedNode.NodeType} row in Audio Project Viewer table for {selectedNode.Name}");
        }

        private void LoadDataGrid(NodeType selectedNodeType)
        {
            var dataGridService = _audioProjectViewerDataGridServiceFactory.GetService(AudioProjectDataGrid.Viewer, selectedNodeType);
            dataGridService.LoadDataGrid(AudioProjectViewerDataGrid);
        }

        partial void OnAudioProjectViewerDataGridChanged(DataTable value)
        {
            if (AudioProjectViewerDataGrid != null && AudioProjectViewerDataGrid.Rows.Count > 0)
                SetPasteEnablement();
        }

        public void SetCopyEnablement()
        {
            if (SelectedRows != null)
                IsCopyEnabled = SelectedRows.AsEnumerable().Any();
        }

        public void SetPasteEnablement()
        {
            IsPasteEnabled = true;

            if (_copiedRows.Count == 0)
            {
                IsPasteEnabled = false;
                return;
            }

            var viewerColumns = AudioProjectViewerDataGrid.Columns
                .Cast<DataColumn>()
                .Select(col => col.ColumnName)
                .ToList();

            var firstRow = _copiedRows[0];
            var rowColumns = firstRow.Table.Columns
                .Cast<DataColumn>()
                .Select(col => col.ColumnName)
                .ToList();

            var schemaMatches = viewerColumns.Count == rowColumns.Count && viewerColumns.All(column => rowColumns.Contains(column));
            if (!schemaMatches)
            {
                IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = _copiedRows
                .Any(copied => AudioProjectViewerDataGrid.AsEnumerable()
                    .Any(viewer => viewerColumns.All(
                        column => Equals(copied[column], viewer[column]))));

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.IsDialogueEvent())
            {
                var dialogueEventStateGroups = _audioRepository
                    .QualifiedStateGroupLookupByStateGroupByDialogueEvent[selectedNode.Name]
                    .Select(kvp => DataGridHelpers.DuplicateUnderscores(kvp.Key))
                    .ToList();

                var copiedStateGroups = rowColumns;

                var areStateGroupsEqual = dialogueEventStateGroups
                    .SequenceEqual(copiedStateGroups);

                IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }


        [RelayCommand] public void CopyRows()
        {
            if (!IsCopyEnabled)
                return;

            _copiedRows = SelectedRows;
            SetPasteEnablement();
        }

        [RelayCommand]  public void PasteRows()
        {
            if (!IsPasteEnabled)
                return;

            foreach (var row in _copiedRows)
            {
                _audioProjectUICommandFactory.Create(AudioProjectCommandAction.AddToAudioProject, _audioEditorService.SelectedExplorerNode.NodeType).Execute(row);
                _eventHub.Publish(new AddViewerTableRowEvent(row));
                _eventHub.Publish(new SetEditorAddRowButtonEnablementEvent());
            }

            SetPasteEnablement();
        }

        public void SetButtonEnablement()
        {
            ResetButtonEnablement();

            if (SelectedRows == null)
            {
                IsUpdateRowButtonEnabled = false;
                IsRemoveRowButtonEnabled = false;
            }
            else if (SelectedRows.Count == 1)
            {
                IsUpdateRowButtonEnabled = true;
                IsRemoveRowButtonEnabled = true;
            }
            else if (SelectedRows.Count > 1)
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
