using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly AudioProjectDataServiceFactory _audioProjectDataServiceFactory;

        private readonly ILogger _logger = Logging.Create<AudioProjectEditorViewModel>();

        public string DisplayName { get; set; } = "Audio Project Editor";

        [ObservableProperty] private string _audioProjectEditorLabel;
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectEditorDataGrid;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (
            IEventHub eventHub,
            IAudioEditorService audioEditorService,
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory,
            AudioProjectDataServiceFactory audioProjectDataServiceFactory)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;
            _audioProjectDataServiceFactory = audioProjectDataServiceFactory;

            AudioProjectEditorLabel = $"{DisplayName}";

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
            _eventHub.Register<ItemEditedEvent>(this, OnItemEdited);
        }

        public void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
        {
            ResetAudioProjectEditorLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
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
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            // Clear data grid to ensure there's only one row in the editor
            DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);

            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.SetDataGridData();
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);
                LoadDataGrid(selectedNode.NodeType);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank && selectedNode.NodeType != NodeType.DialogueEvent && selectedNode.NodeType != NodeType.StateGroup)
                return;

            var dataService = _audioProjectDataServiceFactory.GetService(selectedNode.NodeType);
            dataService.AddToAudioProject();

            _eventHub.Publish(new ItemAddedEvent()); // Publish after the data is added to the AudioProject so it can be loaded when the data grid loading functions are called

            // Clear data grid to ensure there's only one row in the editor
            DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);

            // Reset the data grid row
            var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
            dataGridService.InitialiseDataGridData();

            SetAddRowButtonEnablement(); // Handle this last because the row needs to be cleared before being checked

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        public void SetAddRowButtonEnablement()
        {
            ResetAddRowButtonEnablement();

            if (_audioEditorService.GetEditorDataGrid().Count == 0)
                return;

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.StateGroup)
            {
                if (_audioEditorService.AudioSettingsViewModel.AudioFiles.Count == 0)
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
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
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
            var audioProjectEditorData = DataGridHelpers.GetAudioProjectEditorDataGridRow( _audioEditorService)
                .ToList();

            var rowExists = _audioEditorService.GetViewerDataGrid()
                .Any(dictionary => audioProjectEditorData.SequenceEqual(dictionary));

            return rowExists;
        }

        private bool CheckIfAnyEmptyCells()
        {
            var dataGridRow = _audioEditorService.GetEditorDataGrid()[0];
            var emptyColumns = dataGridRow
                .Where(kvp => kvp.Key != DataGridConfiguration.ActionTypeColumn && kvp.Value is string value && string.IsNullOrEmpty(value))
                .ToList();

            if (emptyColumns.Count > 0)
                return true;
            else
                return false;
        }

        public void SelectMovieFile()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".ca_vp8"]);
            if (result.Result)
            {
                var movieFilePath = _packFileService.GetFullPath(result.File);

                var editorRow = _audioEditorService.GetEditorDataGrid()[0];
                var actionType = AudioProjectHelpers.GetActionTypeFromDataGridRow(editorRow);

                var rowData = new Dictionary<string, string>
                {
                    { DataGridConfiguration.ActionTypeColumn, actionType },
                    { DataGridConfiguration.EventNameColumn, ConvertMovieFilePath(movieFilePath) }
                };

                DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);
                AudioProjectEditorDataGrid.Add(rowData);
            }
        }

        public static string ConvertMovieFilePath(string filePath)
        {
            filePath = filePath.Replace("movies\\", string.Empty);
            filePath = filePath.Replace(Path.GetExtension(filePath), string.Empty);
            filePath = filePath.Replace("\\", "_");
            return "Movie_" + filePath;
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
            DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectEditorDataGridTag));
        }

        public void Close() {}
    }
}
