using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
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
            IAudioEditorService audioEditorService,
            IEditorTableServiceFactory tableServiceFactory,
            IAudioRepository audioRepository)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _tableServiceFactory = tableServiceFactory;
            _audioRepository = audioRepository;

            EditorLabel = $"Audio Project Editor";
            DataGridTag = TableInfo.EditorDataGridTag;

            _audioEditorService.ShowModdedStatesOnly = _showModdedStatesOnly;

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

        private void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
        {
            ResetEditorVisibility();
            ResetEditorLabel();
            ResetButtonEnablement();
            ResetTable();

            var selectedAudioProjectExplorerNode = e.TreeNode;
            if (selectedAudioProjectExplorerNode.IsActionEventSoundBank())
            {
                MakeEditorVisible();
                SetEditorLabel(selectedAudioProjectExplorerNode.Name);
                Load(selectedAudioProjectExplorerNode.NodeType);
            }
            else if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                MakeEditorVisible();
                SetEditorLabel(TableHelpers.DuplicateUnderscores(selectedAudioProjectExplorerNode.Name));
                Load(selectedAudioProjectExplorerNode.NodeType);

                var moddedStatesCount = _audioEditorService.AudioProject.StateGroups.SelectMany(stateGroup => stateGroup.States).Count();
                if (moddedStatesCount > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
            }
            else if (selectedAudioProjectExplorerNode.IsStateGroup())
            {
                MakeEditorVisible();
                SetEditorLabel(TableHelpers.DuplicateUnderscores(selectedAudioProjectExplorerNode.Name));
                Load(selectedAudioProjectExplorerNode.NodeType);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedAudioProjectExplorerNode.NodeType}: {selectedAudioProjectExplorerNode.Name}");

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

            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Added {selectedAudioProjectExplorerNode.NodeType} row to Audio Project Editor table for {selectedAudioProjectExplorerNode.Name} ");
        }

        private void OnEditorDataGridColumnAddRequested(EditorDataGridColumnAddRequestedEvent e) => AddDataGridColumns(e.Column);

        private void AddDataGridColumns(DataGridColumn column)
        {
            var columnNames = DataGridColumns
                .Select(column => column.Header?.ToString() ?? string.Empty)
                .ToList();

            if (!columnNames.Contains(column.Header))
                DataGridColumns.Add(column);
        }

        private void OnEditorTableRowAddedToViewer(EditorTableRowAddedToViewerEvent e)
        {
            // Clear table to ensure there's only one row
            Table.Clear();

            // Re-initialise table
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            var dataGridService = _tableServiceFactory.GetService(selectedAudioProjectExplorerNode.NodeType);
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

        private void OnAudioFilesChanged(AudioFilesChangedEvent e) => SetAudioFiles(e.AudioFiles);

        private void SetAudioFiles(ObservableCollection<AudioFile> audioFiles)
        {
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;

            if (selectedAudioProjectExplorerNode.IsActionEventSoundBank() 
                && selectedAudioProjectExplorerNode.Name != SoundBanks.MoviesDisplayString 
                && audioFiles.Count == 1)
            {
                var row = Table.Rows[0];
                var wavFileName = Path.GetFileNameWithoutExtension(audioFiles[0].FileName);
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
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                Table.Clear();
                Load(selectedAudioProjectExplorerNode.NodeType);
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

            var areViewerCellsEmpty = AreViewerCellsEmpty();
            if (areViewerCellsEmpty)
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
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (!selectedAudioProjectExplorerNode.IsStateGroup())
            {
                if (_audioEditorService.AudioFiles.Count == 0)
                    return false;
            }

            return true;
        }

        private bool DoesRowExist()
        {
            var row = Table.Rows[0];
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsActionEventSoundBank())
            {
                var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
                var actionEvent = _audioEditorService.AudioProject.GetActionEvent(actionEventName);
                if (actionEvent != null)
                    return true;
            }
            else if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(selectedAudioProjectExplorerNode.Name);
                var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEvent.Name);
                var statePath = dialogueEvent.GetStatePath(statePathName);
                if (statePath != null)
                    return true;
            }
            else if (selectedAudioProjectExplorerNode.IsStateGroup())
            {
                var stateGroup = _audioEditorService.AudioProject.GetStateGroup(selectedAudioProjectExplorerNode.Name);
                var stateName = TableHelpers.GetStateNameFromRow(row);
                var state = stateGroup.GetState(stateName);
                if (state != null)
                    return true;
            }
            return false;
        }

        private bool AreViewerCellsEmpty()
        {
            var row = Table.Rows[0];
            return Table.Columns
                .Cast<DataColumn>()
                .Any(column => row.IsNull(column)
                    || (row[column] is string value 
                    && (string.IsNullOrEmpty(value) || value == "Play_")));
        }

        private void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                IsShowModdedStatesCheckBoxVisible = true;

                var moddedStatesCount = _audioEditorService.AudioProject.StateGroups
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
