using System;
using System.Data;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
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
        private readonly IAudioProjectUICommandFactory _audioProjectUICommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly IAudioRepository _audioRepository;

        private readonly ILogger _logger = Logging.Create<AudioProjectEditorViewModel>();

        [ObservableProperty] private string _audioProjectEditorLabel;
        [ObservableProperty] private DataTable _audioProjectEditorDataGrid;
        [ObservableProperty] private string _audioProjectEditorDataGridTag;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (
            IAudioProjectUICommandFactory audioProjectUICommandFactory,
            IEventHub eventHub,
            IAudioEditorService audioEditorService,
            IDataGridServiceFactory audioEditorDataGridServiceFactory,
            IAudioRepository audioRepository)
        {
            _audioProjectUICommandFactory = audioProjectUICommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _audioProjectEditorDataGridServiceFactory = audioEditorDataGridServiceFactory;
            _audioRepository = audioRepository;

            AudioProjectEditorLabel = $"Audio Project Editor";
            AudioProjectEditorDataGridTag = _audioEditorService.AudioProjectEditorDataGridTag;

            _audioEditorService.ShowModdedStatesOnly = _showModdedStatesOnly;

            _eventHub.Register<InitialiseViewModelDataEvent>(this, InitialiseData);
            _eventHub.Register<ResetViewModelDataEvent>(this, ResetData);
            _eventHub.Register<AddEditorTableColumnEvent>(this, AddTableColumn);
            _eventHub.Register<AddEditorTableRowEvent>(this, AddTableRow);
            _eventHub.Register<EditViewerRowEvent>(this, OnViewerRowEdited);
            _eventHub.Register<AudioFilesSetEvent>(this, OnAudioFilesSet);
            _eventHub.Register<TextboxTextChangedEvent>(this, OnTextboxTextChanged);
            _eventHub.Register<SelectMovieFileEvent>(this, OnMovieFileSelected);
            _eventHub.Register<ExplorerNodeSelectedEvent>(this, OnSelectedExplorerNodeChanged);
            _eventHub.Register<SetEditorAddRowButtonEnablementEvent>(this, SetAddRowButtonEnablement);
        }

        public void InitialiseData(InitialiseViewModelDataEvent e)
        {
            AudioProjectEditorDataGrid = new();
        }

        public void ResetData(ResetViewModelDataEvent e)
        {
            AudioProjectEditorDataGrid = null;
        }

        public void AddTableColumn(AddEditorTableColumnEvent addTableColumnEvent)
        {
            var column = addTableColumnEvent.Column;
            if (!AudioProjectEditorDataGrid.Columns.Contains(column.ColumnName))
            {
                AudioProjectEditorDataGrid.Columns.Add(column);
                _logger.Here().Information($"Added {_audioEditorService.SelectedExplorerNode.NodeType} column to Audio Project Editor table for {_audioEditorService.SelectedExplorerNode.Name} ");
            }
        }

        public void AddTableRow(AddEditorTableRowEvent addEditorRowEvent)
        {
            var row = addEditorRowEvent.Row;
            var destination = AudioProjectEditorDataGrid;

            if (row.Table == destination)
                destination.Rows.Add(row);
            else
                destination.ImportRow(row);

            _logger.Here().Information($"Added {_audioEditorService.SelectedExplorerNode.NodeType} row to Audio Project Editor table for {_audioEditorService.SelectedExplorerNode.Name} ");
        }

        public void OnViewerRowEdited(EditViewerRowEvent itemEditedEvent)
        {
            // Clear data grid to ensure there's only one row in the editor
            AudioProjectEditorDataGrid.Clear();

            _eventHub.Publish(new AddEditorTableRowEvent(itemEditedEvent.Row));
        }

        public void OnAudioFilesSet(AudioFilesSetEvent audioFilesSetEvent)
        {
            var audioFiles = audioFilesSetEvent.AudioFiles;
            var selectedNode = _audioEditorService.SelectedExplorerNode;

            if (selectedNode.IsActionEventSoundBank() &&
                selectedNode.Name != SoundBanks.MoviesDisplayString &&
                audioFiles.Count == 1)
            {
                var row = AudioProjectEditorDataGrid.Rows[0];
                var wavFileName = Path.GetFileNameWithoutExtension(audioFiles[0].FileName);
                var eventName = $"Play_{wavFileName}";
                row[DataGridTemplates.EventColumn] = eventName;
            }

            SetAddRowButtonEnablement();
        }

        public void OnTextboxTextChanged(TextboxTextChangedEvent e) => SetAddRowButtonEnablement();

        public void OnMovieFileSelected(SelectMovieFileEvent selectMovieFileEvent)
        {
            var row = AudioProjectEditorDataGrid.Rows[0];
            var eventName = ConvertMovieFilePath(selectMovieFileEvent.MovieFilePath);
            row[DataGridTemplates.EventColumn] = eventName;
        }

        public static string ConvertMovieFilePath(string filePath)
        {
            filePath = filePath.Replace("movies\\", string.Empty);
            filePath = filePath.Replace(Path.GetExtension(filePath), string.Empty);
            filePath = filePath.Replace("\\", "_");
            return $"Play_Movie_{filePath}";
        }

        public void OnSelectedExplorerNodeChanged(ExplorerNodeSelectedEvent explorerNodeSelectedEvent)
        {
            ResetAudioProjectEditorLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = explorerNodeSelectedEvent.TreeNode;
            if (selectedNode.IsActionEventSoundBank())
            {
                SetAudioProjectEditorLabel(selectedNode.Name);
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.IsDialogueEvent())
            {
                SetAudioProjectEditorLabel(DataGridHelpers.DuplicateUnderscores(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);

                var moddedStatesCount = _audioEditorService.AudioProject.StateGroups.SelectMany(stateGroup => stateGroup.States).Count();
                if (moddedStatesCount > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
            }
            else if (selectedNode.IsStateGroup())
            {
                SetAudioProjectEditorLabel(DataGridHelpers.DuplicateUnderscores(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);
            }
            else
                return;

            _logger.Here().Information($"Loaded {selectedNode.NodeType}: {selectedNode.Name}");

            SetAddRowButtonEnablement();
            SetShowModdedStatesOnlyButtonEnablementAndVisibility();
        }

        private void LoadDataGrid(NodeType selectedNodeType)
        {
            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(AudioProjectDataGrid.Editor, selectedNodeType);
            dataGridService.LoadDataGrid(AudioProjectEditorDataGrid);
        }

        public void SetAddRowButtonEnablement(SetEditorAddRowButtonEnablementEvent setEnablementEvent) => SetAddRowButtonEnablement();

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.IsDialogueEvent())
            {
                AudioProjectEditorDataGrid.Clear();
                LoadDataGrid(selectedNode.NodeType);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Rows.Count == 0)
                return;

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            var row = AudioProjectEditorDataGrid.Rows[0];

            // Store the row data in the Audio Project
            _audioProjectUICommandFactory.Create(AudioProjectCommandAction.AddToAudioProject, selectedNode.NodeType).Execute(row);

            // Display the row data in the viewer
            _eventHub.Publish(new AddViewerTableRowEvent(row)); 

            // Clear data grid to ensure there's only one row in the editor
            AudioProjectEditorDataGrid.Clear();

            // Reset the data grid row
            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(AudioProjectDataGrid.Editor, selectedNode.NodeType);
            dataGridService.SetInitialDataGridData(AudioProjectEditorDataGrid);

            SetAddRowButtonEnablement(); // Handle this last because the row needs to be cleared before being checked

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        public void SetAddRowButtonEnablement()
        {
            ResetAddRowButtonEnablement();

            if (AudioProjectEditorDataGrid.Rows.Count == 0)
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
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (!selectedNode.IsStateGroup())
            {
                if (_audioEditorService.AudioFiles.Count == 0)
                    return false;
            }

            return true;
        }

        private bool DoesRowExist()
        {
            var editorRow = AudioProjectEditorDataGrid.Rows[0];
            var selectedExplorerNode = _audioEditorService.SelectedExplorerNode;
            if (selectedExplorerNode.IsActionEventSoundBank())
            {
                var actionEventName = DataGridHelpers.GetActionEventNameFromRow(editorRow);
                var actionEvent = _audioEditorService.AudioProject.GetActionEvent(actionEventName);
                if (actionEvent != null)
                    return true;
            }
            else if (selectedExplorerNode.IsDialogueEvent())
            {
                var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(_audioEditorService.SelectedExplorerNode.Name);
                var statePath = dialogueEvent.GetStatePath(_audioRepository, editorRow);
                if (statePath != null)
                    return true;
            }
            else if (selectedExplorerNode.IsStateGroup())
            {
                var stateGroup = _audioEditorService.AudioProject.GetStateGroup(selectedExplorerNode.Name);
                var stateName = DataGridHelpers.GetStateNameFromRow(editorRow);
                var state = stateGroup.GetState(stateName);
                if (state != null)
                    return true;
            }
            return false;
        }

        private bool AreViewerCellsEmpty()
        {
            var row = AudioProjectEditorDataGrid.Rows[0];
            return AudioProjectEditorDataGrid.Columns
                .Cast<DataColumn>()
                .Any(column => 
                    row.IsNull(column) 
                    || (row[column] is string value && (string.IsNullOrEmpty(value) || value == "Play_")));
        }

        public void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.IsDialogueEvent())
            {
                IsShowModdedStatesCheckBoxVisible = true;

                var moddedStatesCount = _audioEditorService.AudioProject.StateGroups.SelectMany(stateGroup => stateGroup.States).Count();
                if (moddedStatesCount > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
                else if (moddedStatesCount == 0)
                    IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                IsShowModdedStatesCheckBoxVisible = false;
        }

        public void SetAudioProjectEditorLabel(string label)
        {
            AudioProjectEditorLabel = $"Audio Project Editor {label}";
        }

        public void ResetAudioProjectEditorLabel()
        {
            AudioProjectEditorLabel = $"Audio Project Editor";
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

        public void ResetDataGrid()
        {
            AudioProjectEditorDataGrid = new DataTable();
            var dataGrid = DataGridHelpers.GetDataGridFromTag(AudioProjectEditorDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
        }
    }
}
