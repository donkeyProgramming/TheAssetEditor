using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IEditorTableServiceFactory _tableServiceFactory;
        private readonly IAudioRepository _audioRepository;

        private readonly ILogger _logger = Logging.Create<AudioProjectEditorViewModel>();

        [ObservableProperty] private string _editorLabel;
        [ObservableProperty] private DataTable _table = new();
        [ObservableProperty] private ObservableCollection<DataGridColumn> _dataGridColumns = [];
        [ObservableProperty] private string _dataGridTag;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;
        [ObservableProperty] private bool _isEditorVisible = false;

        public AudioProjectEditorViewModel (
            IUiCommandFactory uiCommandFactory,
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IEditorTableServiceFactory tableServiceFactory,
            IAudioRepository audioRepository)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorStateService = audioEditorStateService;
            _tableServiceFactory = tableServiceFactory;
            _audioRepository = audioRepository;

            EditorLabel = $"Audio Project Editor";

            _eventHub.Register<AudioProjectLoadedEvent>(this, OnAudioProjectInitialised);
            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _eventHub.Register<EditorTableColumnAddRequestedEvent>(this, OnEditorTableColumnAddRequested);
            _eventHub.Register<EditorTableRowAddRequestedEvent>(this, OnEditorTableRowAddRequested);
            _eventHub.Register<EditorDataGridColumnAddRequestedEvent>(this, OnEditorDataGridColumnAddRequested);
            _eventHub.Register<EditorTableRowAddedToViewerEvent>(this, OnEditorTableRowAddedToViewer);
            _eventHub.Register<ViewerTableRowEditedEvent>(this, OnViewerTableRowEdited);
            _eventHub.Register<AudioFilesChangedEvent>(this, OnAudioFilesChanged);
            _eventHub.Register<EditorDataGridTextboxTextChangedEvent>(this, OnEditorDataGridTextboxTextChanged);
            _eventHub.Register<MovieFileChangedEvent>(this, OnMovieFileChanged);
            _eventHub.Register<EditorAddRowButtonEnablementUpdateRequestedEvent>(this, OnEditorAddRowButtonEnablementUpdateRequested);
        }

        private void OnAudioProjectInitialised(AudioProjectLoadedEvent e)
        {
            ResetEditorVisibility();
            ResetEditorLabel();
            ResetButtonEnablement();
            ResetTable();
        }

        private void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
        {
            ResetEditorVisibility();
            ResetEditorLabel();
            ResetButtonEnablement();
            ResetTable();

            var selectedAudioProjectExplorerNode = e.TreeNode;
            if (selectedAudioProjectExplorerNode.IsActionEvent())
            {
                MakeEditorVisible();
                SetEditorLabel(selectedAudioProjectExplorerNode.Name);
                Load(selectedAudioProjectExplorerNode.Type);
            }
            else if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                MakeEditorVisible();
                SetEditorLabel(TableHelpers.DuplicateUnderscores(selectedAudioProjectExplorerNode.Name));
                Load(selectedAudioProjectExplorerNode.Type);

                var moddedStatesCount = _audioEditorStateService.AudioProject.StateGroups.SelectMany(stateGroup => stateGroup.States).Count();
                if (moddedStatesCount > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
            }
            else if (selectedAudioProjectExplorerNode.IsStateGroup())
            {
                MakeEditorVisible();
                SetEditorLabel(TableHelpers.DuplicateUnderscores(selectedAudioProjectExplorerNode.Name));
                Load(selectedAudioProjectExplorerNode.Type);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedAudioProjectExplorerNode.Type}: {selectedAudioProjectExplorerNode.Name}");

            SetAddRowButtonEnablement();
            SetShowModdedStatesOnlyButtonEnablementAndVisibility();
        }

        private void OnEditorTableColumnAddRequested(EditorTableColumnAddRequestedEvent e) => AddTableColumn(e.Column);

        private void AddTableColumn(DataColumn column)
        {
            if (!Table.Columns.Contains(column.ColumnName))
                Table.Columns.Add(column);
        }

        private void OnEditorTableRowAddRequested(EditorTableRowAddRequestedEvent e) => AddTableRow(e.Row);

        private void AddTableRow(DataRow row)
        {
            if (row.Table == Table)
                Table.Rows.Add(row);
            else
                Table.ImportRow(row);

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Added {selectedAudioProjectExplorerNode.Type} row to Audio Project Editor table for {selectedAudioProjectExplorerNode.Name} ");
        }

        private void OnEditorDataGridColumnAddRequested(EditorDataGridColumnAddRequestedEvent e) => AddDataGridColumns(e.Column);

        private void AddDataGridColumns(DataGridColumn column)
        {
            if (column is null)
                return;

            // Prevent the same instance being added twice
            if (DataGridColumns.Contains(column))
                return;

            var headerName = column.Header?.ToString() ?? string.Empty;

            // Prevent two different instances with the same header text
            var existingColumn = DataGridColumns
                .FirstOrDefault(col => string.Equals(col.Header?.ToString(), headerName, StringComparison.Ordinal));

            if (existingColumn != null)
                DataGridColumns.Remove(existingColumn);

            DataGridColumns.Add(column);
        }


        private void OnEditorTableRowAddedToViewer(EditorTableRowAddedToViewerEvent e)
        {
            // Clear table to ensure there's only one row
            Table.Clear();

            // Re-initialise table
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            var dataGridService = _tableServiceFactory.GetService(selectedAudioProjectExplorerNode.Type);
            dataGridService.InitialiseTable(Table);

            // Handle enablement last because the row needs to be cleared before being checked
            SetAddRowButtonEnablement();
        }   

        private void OnViewerTableRowEdited(ViewerTableRowEditedEvent e)
        {
            // Clear table to ensure there's only one row
            Table.Clear();

            _eventHub.Publish(new EditorTableRowAddRequestedEvent(e.Row));
        }

        private void OnAudioFilesChanged(AudioFilesChangedEvent e) => SetEventNameFromAudioFile(e.AudioFiles, e.AddToExistingAudioFiles, e.IsSetFromEditedItem);

        private void SetEventNameFromAudioFile(List<AudioFile> audioFiles, bool addToExistingAudioFiles, bool isSetFromEditedItem)
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            var isActionEvent = selectedAudioProjectExplorerNode.IsActionEvent();
            var isNotMoviesActionEvent = selectedAudioProjectExplorerNode.Name != Wh3ActionEventInformation.GetName(Wh3ActionEventType.Movies);
            var hasExistingAudioFiles = _audioEditorStateService.AudioFiles.Count > 0;

            if (isActionEvent
                && !isSetFromEditedItem
                && isNotMoviesActionEvent
                && audioFiles.Count == 1
                && ((hasExistingAudioFiles && !addToExistingAudioFiles) || (!hasExistingAudioFiles && addToExistingAudioFiles)))
            {
                var row = Table.Rows[0];
                var wavFileName = Path.GetFileNameWithoutExtension(audioFiles[0].WavPackFileName);
                var eventName = $"Play_{wavFileName}";
                row[TableInfo.EventColumnName] = eventName;
            }

            SetAddRowButtonEnablement();
        }

        private void OnEditorDataGridTextboxTextChanged(EditorDataGridTextboxTextChangedEvent e) => SetAddRowButtonEnablement();

        private void OnMovieFileChanged(MovieFileChangedEvent e) => SetMovieFilePath(e.MovieFilePath);

        private void SetMovieFilePath(string movieFilePath)
        {
            var relativePath = Path.GetRelativePath("movies", movieFilePath);
            var withoutExtension = Path.ChangeExtension(relativePath, null);
            var slashesToUnderscores = withoutExtension.Replace("\\", "_");
            var eventName = $"Play_Movie_{slashesToUnderscores}";

            var row = Table.Rows[0];
            row[TableInfo.EventColumnName] = eventName;
        }

        public void OnEditorAddRowButtonEnablementUpdateRequested(EditorAddRowButtonEnablementUpdateRequestedEvent e) => SetAddRowButtonEnablement();

        private void Load(AudioProjectTreeNodeType selectedNodeType)
        {
            var tableService = _tableServiceFactory.GetService(selectedNodeType);
            tableService.Load(Table);
        }

        [RelayCommand] public void AddRowToViewer() => _uiCommandFactory.Create<AddEditorRowToViewerCommand>().Execute(Table.Rows[0]);

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            _audioEditorStateService.StoreModdedStatesOnly(ShowModdedStatesOnly);

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                Table.Clear();
                Load(selectedAudioProjectExplorerNode.Type);
            }
        }

        private void SetAddRowButtonEnablement()
        {
            IsAddRowButtonEnabled = false;

            if (Table.Rows.Count == 0)
                return;

            var areAudioFilesSet = AreAudioFilesSet();
            if (!areAudioFilesSet)
                return;

            var doesRowExist = DoesRowExist();
            if (doesRowExist)
            {
                IsAddRowButtonEnabled = false;
                return;
            }

            var areEditorCellsEmpty = AreEditorCellsEmpty();
            if (areEditorCellsEmpty)
            {
                IsAddRowButtonEnabled = false;
                return;
            }
            else
            {
                IsAddRowButtonEnabled = true;
                return;
            }
        }

        private bool AreAudioFilesSet()
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (!selectedAudioProjectExplorerNode.IsStateGroup())
            {
                if (_audioEditorStateService.AudioFiles.Count == 0)
                    return false;
            }

            return true;
        }

        private bool DoesRowExist()
        {
            var row = Table.Rows[0];
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsActionEvent())
            {
                var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
                var actionEvent = _audioEditorStateService.AudioProject.GetActionEvent(actionEventName);
                if (actionEvent != null)
                    return true;
            }
            else if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(selectedAudioProjectExplorerNode.Name);
                var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEvent.Name);
                var statePath = dialogueEvent.GetStatePath(statePathName);
                if (statePath != null)
                    return true;
            }
            else if (selectedAudioProjectExplorerNode.IsStateGroup())
            {
                var stateGroup = _audioEditorStateService.AudioProject.GetStateGroup(selectedAudioProjectExplorerNode.Name);
                var stateName = TableHelpers.GetStateNameFromRow(row);
                var state = stateGroup.GetState(stateName);
                if (state != null)
                    return true;
            }
            return false;
        }

        private bool AreEditorCellsEmpty()
        {
            var row = Table.Rows[0];
            return Table.Columns
                .Cast<DataColumn>()
                .Any(column => row.IsNull(column) || (row[column] is string value && (string.IsNullOrEmpty(value) || value == "Play_")));
        }

        private void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                IsShowModdedStatesCheckBoxVisible = true;

                var moddedStatesCount = _audioEditorStateService.AudioProject.StateGroups
                    .SelectMany(stateGroup => stateGroup.States)
                    .Count();
                if (moddedStatesCount > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
                else if (moddedStatesCount == 0)
                    IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                IsShowModdedStatesCheckBoxVisible = false;
        }

        private void ResetTable()
        {
            Table = new DataTable();
            DataGridColumns.Clear();
        }

        private void ResetButtonEnablement()
        {
            IsAddRowButtonEnabled = false;
            IsShowModdedStatesCheckBoxEnabled = false;
        }

        private void SetEditorLabel(string label) => EditorLabel = $"Audio Project Editor - {label}";

        private void ResetEditorLabel() => EditorLabel = $"Audio Project Editor";

        private void MakeEditorVisible() => IsEditorVisible = true;

        private void ResetEditorVisibility() => IsEditorVisible = false;
    }
}
