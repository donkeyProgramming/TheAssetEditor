using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.UICommands;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject
    {
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory;
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
            IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
            IEventHub eventHub,
            IAudioEditorService audioEditorService,
            IEditorTableServiceFactory tableServiceFactory,
            IAudioRepository audioRepository)
        {
            _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _tableServiceFactory = tableServiceFactory;
            _audioRepository = audioRepository;

            EditorLabel = $"Audio Project Editor";
            DataGridTag = TableInfo.EditorDataGridTag;

            _audioEditorService.ShowModdedStatesOnly = _showModdedStatesOnly;

            _eventHub.Register<EditorTableColumnAddedEvent>(this, OnEditorTableColumnAdded);
            _eventHub.Register<EditorTableRowAddedEvent>(this, OnEditorTableRowAdded);
            _eventHub.Register<ViewerTableRowEditedEvent>(this, OnViewerRowEdited);
            _eventHub.Register<AudioFilesChangedEvent>(this, OnAudioFilesChanged);
            _eventHub.Register<EditorDataGridTextboxTextChangedEvent>(this, OnEditorDataGridTextboxTextChanged);
            _eventHub.Register<MovieFileChangedEvent>(this, OnMovieFileChanged);
            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _eventHub.Register<EditorAddRowButtonEnablementChangedEvent>(this, OnEditorAddRowButtonEnablementChanged);
            _eventHub.Register<EditorDataGridColumnAddedEvent>(this, OnEditorDataGridColumnAdded);
        }

        private void OnEditorTableColumnAdded(EditorTableColumnAddedEvent e) => AddTableColumn(e.Column);

        private void AddTableColumn(DataColumn column)
        {
            if (!Table.Columns.Contains(column.ColumnName))
                Table.Columns.Add(column);
        }

        public void OnEditorTableRowAdded(EditorTableRowAddedEvent e) => AddTableRow(e.Row);

        private void AddTableRow(DataRow row)
        {
            if (row.Table == Table)
                Table.Rows.Add(row);
            else
                Table.ImportRow(row);

            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Added {selectedAudioProjectExplorerNode.NodeType} row to Audio Project Editor table for {selectedAudioProjectExplorerNode.Name} ");
        }

        public void OnViewerRowEdited(ViewerTableRowEditedEvent e)
        {
            // Clear table to ensure there's only one row
            Table.Clear();

            _eventHub.Publish(new EditorTableRowAddedEvent(e.Row));
        }

        private void OnAudioFilesChanged(AudioFilesChangedEvent e) => SetAudioFiles(e.AudioFiles);

        public void SetAudioFiles(ObservableCollection<AudioFile> audioFiles)
        {
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;

            if (selectedAudioProjectExplorerNode.IsActionEventSoundBank() &&
                selectedAudioProjectExplorerNode.Name != SoundBanks.MoviesDisplayString &&
                audioFiles.Count == 1)
            {
                var row = Table.Rows[0];
                var wavFileName = Path.GetFileNameWithoutExtension(audioFiles[0].FileName);
                var eventName = $"Play_{wavFileName}";
                row[TableInfo.EventColumnName] = eventName;
            }

            SetAddRowButtonEnablement();
        }

        public void OnEditorDataGridTextboxTextChanged(EditorDataGridTextboxTextChangedEvent e) => SetAddRowButtonEnablement();

        private void OnMovieFileChanged(MovieFileChangedEvent e) => SetMovieFilePath(e.MovieFilePath);

        private void SetMovieFilePath(string movieFilePath)
        {
            var row = Table.Rows[0];
            var eventName = ConvertMovieFilePath(movieFilePath);
            row[TableInfo.EventColumnName] = eventName;
        }

        public static string ConvertMovieFilePath(string filePath)
        {
            filePath = filePath.Replace("movies\\", string.Empty);
            filePath = filePath.Replace(Path.GetExtension(filePath), string.Empty);
            filePath = filePath.Replace("\\", "_");
            return $"Play_Movie_{filePath}";
        }

        public void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
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

        private void Load(AudioProjectExplorerTreeNodeType selectedNodeType)
        {
            var tableService = _tableServiceFactory.GetService(selectedNodeType);
            tableService.Load(Table);
        }

        public void OnEditorAddRowButtonEnablementChanged(EditorAddRowButtonEnablementChangedEvent e) => SetAddRowButtonEnablement();

        private void OnEditorDataGridColumnAdded(EditorDataGridColumnAddedEvent e) => AddDataGridColumns(e.Column);

        private void AddDataGridColumns(DataGridColumn column)
        {
            var columnNames = DataGridColumns
                .Select(column => column.Header?.ToString() ?? string.Empty)
                .ToList();

            if (!columnNames.Contains(column.Header))
                DataGridColumns.Add(column);
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode.IsDialogueEvent())
            {
                Table.Clear();
                Load(selectedAudioProjectExplorerNode.NodeType);
            }
        }

        [RelayCommand] public void AddRowToViewer()
        {
            if (Table.Rows.Count == 0)
                return;

            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            var row = Table.Rows[0];

            // Store the row data in the Audio Project
            _audioProjectMutationUICommandFactory.Create(MutationType.Add, selectedAudioProjectExplorerNode.NodeType).Execute(row);

            // Display the row data in the Viewer
            _eventHub.Publish(new ViewerTableRowAddedEvent(row));

            // Clear table to ensure there's only one row
            Table.Clear();

            // Re-initialise table
            var dataGridService = _tableServiceFactory.GetService(selectedAudioProjectExplorerNode.NodeType);
            dataGridService.InitialiseTable(Table);

            // Handle enablement last because the row needs to be cleared before being checked
            SetAddRowButtonEnablement();
        }

        public void SetAddRowButtonEnablement()
        {
            ResetAddRowButtonEnablement();

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

        public void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
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

        public void SetEditorLabel(string label)
        {
            EditorLabel = $"Audio Project Editor - {label}";
        }

        public void ResetEditorLabel()
        {
            EditorLabel = $"Audio Project Editor";
        }

        public void ResetButtonEnablement()
        {
            ResetAddRowButtonEnablement();
            ResetShowModdedStatesCheckBoxEnablement();
        }

        public void ResetAddRowButtonEnablement()
        {
            IsAddRowButtonEnabled = false;
        }

        public void ResetShowModdedStatesCheckBoxEnablement()
        {
            IsShowModdedStatesCheckBoxEnabled = false;
        }

        public void ResetTable()
        {
            Table = new DataTable();
            DataGridColumns.Clear();
        }

        public void MakeEditorVisible()
        {
            IsEditorVisible = true;
        }

        public void ResetEditorVisibility()
        {
            IsEditorVisible = false;
        }
    }
}
