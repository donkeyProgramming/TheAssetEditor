using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer.Table;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IViewerTableServiceFactory _tableServiceFactory;
        private readonly IAudioRepository _audioRepository;

        private readonly ILogger _logger = Logging.Create<AudioProjectViewerViewModel>();

        [ObservableProperty] private string _viewerLabel;
        [ObservableProperty] private string _dataGridTag;
        [ObservableProperty] private DataTable _table = new();
        [ObservableProperty] private ObservableCollection<DataGridColumn> _dataGridColumns = [];
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] public List<DataRow> _selectedRows = [];
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;
        [ObservableProperty] private bool _isContextMenuPasteVisible = false;
        [ObservableProperty] private bool _isContextMenuCopyVisible = false;
        [ObservableProperty] private bool _isViewerVisible = false;

        public AudioProjectViewerViewModel(
            IUiCommandFactory uiCommandFactory,
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IViewerTableServiceFactory tableServiceFactory,
            IAudioRepository audioRepository)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorStateService = audioEditorStateService;
            _tableServiceFactory = tableServiceFactory;
            _audioRepository = audioRepository;

            ViewerLabel = $"Audio Project Viewer";

            _eventHub.Register<AudioProjectLoadedEvent>(this, OnAudioProjectInitialised);
            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _eventHub.Register<ViewerTableColumnAddRequestedEvent>(this, OnViewerTableColumnAddRequested);
            _eventHub.Register<ViewerTableRowAddRequestedEvent>(this, OnViewerTableRowAddRequested);
            _eventHub.Register<ViewerTableRowRemoveRequestedEvent>(this, OnViewerTableRowRemoveRequested);
            _eventHub.Register<PasteViewerRowsShortcutActivatedEvent>(this, OnPasteViewerRowsShortcutActivated);
            _eventHub.Register<ViewerDataGridColumnAddedEvent>(this, OnViewerDataGridColumnAdded);
        }

        private void OnAudioProjectInitialised(AudioProjectLoadedEvent e)
        {
            ResetViewerVisibility();
            ResetViewerLabel();
            ResetButtonEnablement();
            ResetContextMenuVisibility();
            ResetTable();
        }

        public void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
        {
            ResetViewerVisibility();
            ResetViewerLabel();
            ResetButtonEnablement();
            ResetContextMenuVisibility();
            ResetTable();

            var selectedExplorerNode = e.TreeNode;
            if (selectedExplorerNode.IsActionEvent())
            {
                MakeViewerVisible();
                SetViewerLabel(selectedExplorerNode.Name);
                Load(selectedExplorerNode.Type);
            }
            else if (selectedExplorerNode.IsDialogueEvent())
            {
                MakeViewerVisible();
                SetViewerLabel(TableHelpers.DuplicateUnderscores(selectedExplorerNode.Name));
                Load(selectedExplorerNode.Type);

                SetContextMenuVisibility();
                SetCopyEnablement();
                SetPasteEnablement();
            }
            else if (selectedExplorerNode.IsStateGroup())
            {
                MakeViewerVisible();
                SetViewerLabel(TableHelpers.DuplicateUnderscores(selectedExplorerNode.Name));
                Load(selectedExplorerNode.Type);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedExplorerNode.Type}: {selectedExplorerNode.Name}");
        }

        private void OnViewerTableColumnAddRequested(ViewerTableColumnAddRequestedEvent e) => AddTableColumn(e.Column);

        public void AddTableColumn(DataColumn column)
        {
            if (!Table.Columns.Contains(column.ColumnName))
                Table.Columns.Add(column);
        }

        private void OnViewerTableRowAddRequested(ViewerTableRowAddRequestedEvent e) => AddTableRow(e.Row);

        public void AddTableRow(DataRow row)
        {
            var selectedExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedExplorerNode.IsActionEvent())
                TableHelpers.InsertRowAlphabeticallyByActionEventName(Table, row);
            else if (selectedExplorerNode.IsDialogueEvent())
                TableHelpers.InsertRowAlphabeticallyByStatePathName(Table, row, _audioRepository, selectedExplorerNode.Name);
            else if (selectedExplorerNode.IsStateGroup())
                TableHelpers.InsertRowAlphabeticallyByStateName(Table, row);

            _logger.Here().Information($"Added {selectedExplorerNode.Type} row to Audio Project Viewer table for {selectedExplorerNode.Name}");
        }

        private void OnViewerTableRowRemoveRequested(ViewerTableRowRemoveRequestedEvent e) => RemoveTableRow(e.Row);

        public void RemoveTableRow(DataRow row)
        {
            Table.Rows.Remove(row);

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Removed {selectedAudioProjectExplorerNode.Type} row from Audio Project Viewer table for {selectedAudioProjectExplorerNode.Name}");
        }

        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (_audioEditorStateService.SelectedViewerRows != null && _audioEditorStateService.SelectedViewerRows.Count > 0)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                    CopyRows();

                if (e.Key == Key.Delete || e.Key == Key.Back)
                    _uiCommandFactory.Create<RemoveViewerRowsCommand>().Execute(_audioEditorStateService.SelectedViewerRows);

                e.Handled = true;
            }
        }

        [RelayCommand] public void CopyRows()
        {
            if (!IsCopyEnabled)
                return;

            _audioEditorStateService.StoreCopiedViewerRows(SelectedRows);
            SetPasteEnablement();
        }

        public void OnPasteViewerRowsShortcutActivated(PasteViewerRowsShortcutActivatedEvent e) => PasteRows();

        [RelayCommand] public void PasteRows()
        {
            if (!IsPasteEnabled)
                return;

            _uiCommandFactory.Create<PasteViewerRowsCommand>().Execute(_audioEditorStateService.CopiedViewerRows);

            SetPasteEnablement();
        }

        private void OnViewerDataGridColumnAdded(ViewerDataGridColumnAddedEvent e) => AddDataGridColumns(e.Column);

        private void AddDataGridColumns(DataGridColumn column)
        {
            if (column is null)
                return;

            // Prevent the same instance being added twice
            if (DataGridColumns.Contains(column))
                return;

            // Prevent two different instances with the same header text
            var headerText = column.Header?.ToString() ?? string.Empty;
            if (DataGridColumns.Any(col => string.Equals(col.Header?.ToString(), headerText, StringComparison.Ordinal)))
                return;

            DataGridColumns.Add(column);
        }


        partial void OnSelectedRowsChanged(List<DataRow> value)
        {
            _audioEditorStateService.StoreSelectedViewerRows(SelectedRows);

            _eventHub.Publish(new ViewerTableRowSelectionChangedEvent());

            SetButtonEnablement();

            if (IsContextMenuCopyVisible && IsContextMenuPasteVisible)
                SetCopyEnablement();
        }

        [RelayCommand] public void RemoveRow() => _uiCommandFactory.Create<RemoveViewerRowsCommand>().Execute(SelectedRows);

        [RelayCommand] public void EditRow() => _uiCommandFactory.Create<EditViewerRowCommand>().Execute(SelectedRows);

        private void Load(AudioProjectTreeNodeType selectedNodeType)
        {
            var tableService = _tableServiceFactory.GetService(selectedNodeType);
            tableService.Load(Table);
        }

        partial void OnTableChanged(DataTable value)
        {
            if (Table != null && Table.Rows.Count > 0)
                SetPasteEnablement();
        }

        public void SetContextMenuVisibility()
        {
            IsContextMenuCopyVisible = true;
            IsContextMenuPasteVisible = true;
        }

        public void SetCopyEnablement()
        {
            if (SelectedRows != null && IsContextMenuCopyVisible)
                IsCopyEnabled = SelectedRows.AsEnumerable().Any();
        }

        public void SetPasteEnablement()
        {
            if (!IsContextMenuPasteVisible)
            {
                IsPasteEnabled = false;
                return;
            }

            if (_audioEditorStateService.CopiedViewerRows == null || _audioEditorStateService.CopiedViewerRows.Count == 0)
            {
                IsPasteEnabled = false;
                return;
            }

            var viewerColumns = Table.Columns
                .Cast<DataColumn>()
                .Select(col => col.ColumnName)
                .ToList();

            var firstRow = _audioEditorStateService.CopiedViewerRows[0];
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

            var areAnyCopiedRowsInDataGrid = _audioEditorStateService.CopiedViewerRows
                .Any(copied => Table.AsEnumerable()
                    .Any(viewer => viewerColumns
                    .All(column => Equals(copied[column], viewer[column]))));

            var selectedAudioProjectExplorerNodeName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var dialogueEventStateGroups = _audioRepository
                .QualifiedStateGroupByStateGroupByDialogueEvent[selectedAudioProjectExplorerNodeName]
                .Select(kvp => TableHelpers.DuplicateUnderscores(kvp.Key))
                .ToList();

            var copiedStateGroups = rowColumns;
            var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedStateGroups);
            IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
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

        public void ResetButtonEnablement()
        {
            IsUpdateRowButtonEnabled = false;
            IsRemoveRowButtonEnabled = false;
        }

        public void ResetContextMenuVisibility()
        {
            IsContextMenuCopyVisible = false;
            IsContextMenuPasteVisible = false;
        }

        public void ResetTable()
        {
            Table = new DataTable();
            DataGridColumns.Clear();
            SelectedRows = [];
        }

        public void SetViewerLabel(string label) => ViewerLabel = $"Audio Project Viewer - {label}";

        public void ResetViewerLabel() => ViewerLabel = $"Audio Project Viewer";

        public void MakeViewerVisible() => IsViewerVisible = true;

        public void ResetViewerVisibility() => IsViewerVisible = false;
    }
}
