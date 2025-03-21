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
            _eventHub.Register<SaveDataGridsEvent>(this, SaveDataGrid);
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
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank || selectedNode.NodeType == NodeType.DialogueEvent || selectedNode.NodeType == NodeType.StateGroup)
                return;

            DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);
            DataGridHelpers.AddAudioProjectViewerDataGridDataToAudioProjectEditor(_audioEditorService);
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);

                var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
                dataGridService.LoadDataGrid();
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            _eventHub.Publish(new SaveDataGridsEvent());
            _eventHub.Publish(new ItemAddedEvent());

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.NodeType != NodeType.ActionEventSoundBank || selectedNode.NodeType == NodeType.DialogueEvent || selectedNode.NodeType == NodeType.StateGroup)
                return;

            AddData(selectedNode.NodeType);
            SetAddRowButtonEnablement();

            _logger.Here().Information($"Added {selectedNode.NodeType} item in: {selectedNode.Name}");
        }

        private void AddData(NodeType nodeType)
        {
            var actionEventDataService = _audioProjectDataServiceFactory.GetService(nodeType);
            actionEventDataService.AddToAudioProject();

            // Clear data grid to ensure there's only one row in the editor
            DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);

            // Reset the data grid row
            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(nodeType);
            audioProjectEditorDataGridService.ResetDataGridData();
        }

        public void SetAddRowButtonEnablement()
        {
            _audioEditorService.AudioProjectEditorViewModel.ResetAddRowButtonEnablement();

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
            var emptyColumns = _audioEditorService.GetEditorDataGrid()[0]
                .Where(kvp => kvp.Value is string value && string.IsNullOrEmpty(value))
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
                if (AudioProjectEditorDataGrid[0].ContainsKey("Event"))
                {
                    AudioProjectEditorDataGrid.Clear();
                    var rowData = new Dictionary<string, string>
                    {
                        { "Event", ConvertMovieFilePath(movieFilePath) }
                    };
                    AudioProjectEditorDataGrid.Add(rowData);
                }
            }
        }

        public static string ConvertMovieFilePath(string filePath)
        {
            filePath = filePath.Replace("movies\\", string.Empty);
            filePath = filePath.Replace(Path.GetExtension(filePath), string.Empty);
            filePath = filePath.Replace("\\", "_");
            return "Play_Movie_" + filePath;
        }

        public void SetAudioProjectEditorLabel(string label)
        {
            AudioProjectEditorLabel = $"Audio Project Editor {label}";
        }

        public void ResetAudioProjectEditorLabel()
        {
            AudioProjectEditorLabel = $"Audio Project Editor";
        }

        public void SaveDataGrid(SaveDataGridsEvent saveDataGridEvent)
        {
            _audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGrid = AudioProjectEditorDataGrid;
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
