using System;
using System.Data;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.GameSettings.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject
    {
        private readonly IAudioProjectUICommandFactory _audioProjectUICommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectEditorViewModel>();

        [ObservableProperty] private string _audioProjectEditorLabel;

        [ObservableProperty] private DataTable _audioProjectEditorDataGrid;
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";

        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (
            IAudioProjectUICommandFactory audioProjectUICommandFactory,
            IEventHub eventHub,
            IAudioEditorService audioEditorService,
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory)
        {
            _audioProjectUICommandFactory = audioProjectUICommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;

            AudioProjectEditorLabel = $"Audio Project Editor";

            _eventHub.Register<AudioFilesSetEvent>(this, OnAudioFilesSet);



            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _eventHub.Register<ItemEditedEvent>(this, OnItemEdited);
            _eventHub.Register<SetEnablementEvent>(this, SetItemEnablementAfterRemoval);
        }

        public void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
        {
            ResetAudioProjectEditorLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType == NodeType.ActionEventSoundBank)
            {
                SetAudioProjectEditorLabel(selectedNode.Name);
                LoadDataGrid(selectedNode.NodeType);
            }
            else if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                SetAudioProjectEditorLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));
                LoadDataGrid(selectedNode.NodeType);

                // Rebuild in case any have been added since the Audio Project was initialised
                _audioEditorService.BuildModdedStatesByStateGroupLookup(_audioEditorService.AudioProject.StateGroups, _audioEditorService.ModdedStatesByStateGroupLookup);

                if (_audioEditorService.ModdedStatesByStateGroupLookup.Count > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
            }
            else if (selectedNode.NodeType == NodeType.StateGroup)
            {
                SetAudioProjectEditorLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));
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
            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNodeType);
            dataGridService.LoadDataGrid();
        }

        public void OnItemEdited(ItemEditedEvent itemEditedEvent)
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            // Clear data grid to ensure there's only one row in the editor
            AudioProjectEditorDataGrid.Clear();

            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.SetDataGridData();
        }

        public void SetItemEnablementAfterRemoval(SetEnablementEvent setEnablementEvent) => SetAddRowButtonEnablement();

        public void OnAudioFilesSet(AudioFilesSetEvent audioFilesSetEvent)
        {
            var audioFiles = audioFilesSetEvent.AudioFiles;
            var selectedNode = _audioEditorService.SelectedExplorerNode;

            if (selectedNode.NodeType == NodeType.ActionEventSoundBank &&
                selectedNode.Name != SoundBanks.MoviesDisplayString &&
                audioFiles.Count == 1)
            {
                var row = AudioProjectEditorDataGrid.Rows[0];
                var wavFileName = Path.GetFileNameWithoutExtension(audioFiles[0].Name);
                var eventName = $"Play_{wavFileName}";
                row[DataGridConfiguration.EventNameColumn] = eventName;
            }
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType == NodeType.DialogueEvent)
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
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            var editorRow = AudioProjectEditorDataGrid.Rows[0];

            // Store the row data in the Audio Project
            _audioProjectUICommandFactory.Create(AudioProjectCommandAction.AddToAudioProject, selectedNode.NodeType).Execute(editorRow);

            // Display the row data in the viewer
            _eventHub.Publish(new AddRowEvent(editorRow)); 

            // Clear data grid to ensure there's only one row in the editor
            AudioProjectEditorDataGrid.Clear();

            // Reset the data grid row
            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.InitialiseDataGridData();

            SetAddRowButtonEnablement(); // Handle this last because the row needs to be cleared before being checked

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        public void SetAddRowButtonEnablement()
        {
            ResetAddRowButtonEnablement();

            if (AudioProjectEditorDataGrid.Rows.Count == 0)
                return;

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType != NodeType.StateGroup)
            {
                if (_audioEditorService.AudioFiles.Count == 0)
                    return;
            }

            var rowExistsCheckResult = CheckIfAudioProjectViewerRowExists();
            if (rowExistsCheckResult)
            {
                IsAddRowButtonEnabled = false;
                return;
            }

            var emptyCellsCheckResult = CheckIfAnyEmptyCells();
            if (emptyCellsCheckResult)
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

        public void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                IsShowModdedStatesCheckBoxVisible = true;

                if (_audioEditorService.ModdedStatesByStateGroupLookup.Count > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;
                else if (_audioEditorService.ModdedStatesByStateGroupLookup.Count == 0)
                    IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                IsShowModdedStatesCheckBoxVisible = false;
        }

        private bool CheckIfAudioProjectViewerRowExists()
        {
            var editorRow = AudioProjectEditorDataGrid.Rows[0];
            var viewerTable = _audioEditorService.GetViewerDataGrid();
            return viewerTable.AsEnumerable()
                .Any(viewerRow => viewerRow.ItemArray.SequenceEqual(editorRow.ItemArray)); ;
        }

        private bool CheckIfAnyEmptyCells()
        {
            var row = AudioProjectEditorDataGrid.Rows[0];
            return AudioProjectEditorDataGrid.Columns
                .Cast<DataColumn>()
                .Any(column => 
                    row.IsNull(column) 
                    || (row[column] is string value && (string.IsNullOrEmpty(value) || value == "Play_")));
        }

        public void SelectMovieFile()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".ca_vp8"]);
            if (result.Result)
            {
                var movieFilePath = _packFileService.GetFullPath(result.File);

                var row = AudioProjectEditorDataGrid.Rows[0];

                var eventName = ConvertMovieFilePath(movieFilePath);
                row[DataGridConfiguration.EventNameColumn] = eventName;
            }
        }

        public static string ConvertMovieFilePath(string filePath)
        {
            filePath = filePath.Replace("movies\\", string.Empty);
            filePath = filePath.Replace(Path.GetExtension(filePath), string.Empty);
            filePath = filePath.Replace("\\", "_");
            return $"Play_Movie_{filePath}";
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
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectEditorDataGridTag));
        }
    }
}
