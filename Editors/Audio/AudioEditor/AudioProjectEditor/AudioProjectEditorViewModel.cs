using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
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
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IEventHub _eventHub;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly DataManager _dataManager;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;

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
            IAudioRepository audioRepository,
            IAudioEditorService audioEditorService,
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            DataManager dataManager,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory)
        {
            _eventHub = eventHub;
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _dataManager = dataManager;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;

            AudioProjectEditorLabel = $"{DisplayName}";

            _eventHub.Register<NodeSelectedEvent>(this, OnSelectedNodeChanged);
        }

        public void OnSelectedNodeChanged(NodeSelectedEvent nodeSelectedEvent)
        {
            ResetAudioProjectEditorLabel();
            ResetButtonEnablement();
            ResetDataGrid();

            var selectedNode = nodeSelectedEvent.SelectedNode;
            if (selectedNode.NodeType == NodeType.ActionEventSoundBank)
            {
                SetAudioProjectEditorLabel(selectedNode.Name);

                var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded Action Event SoundBank: {selectedNode.Name}");
            }
            else if (selectedNode.NodeType == NodeType.DialogueEvent)
            {
                SetAudioProjectEditorLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));

                // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
                _audioEditorService.BuildModdedStatesByStateGroupLookup(_audioEditorService.AudioProject.StateGroups, _audioEditorService.ModdedStatesByStateGroupLookup);

                if (_audioEditorService.ModdedStatesByStateGroupLookup.Count > 0)
                    IsShowModdedStatesCheckBoxEnabled = true;

                var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded Dialogue Event: {selectedNode.Name}");
            }
            else if (selectedNode.NodeType == NodeType.StateGroup)
            {
                SetAudioProjectEditorLabel(DataGridHelpers.AddExtraUnderscoresToString(selectedNode.Name));

                var dataGridService = _audioProjectEditorDataGridServiceFactory.GetService(selectedNode.NodeType);
                dataGridService.LoadDataGrid(AudioEditorViewModel);

                _logger.Here().Information($"Loaded State Group: {selectedNode.Name}");
            }

            SetAddRowButtonEnablement();
            SetShowModdedStatesOnlyButtonEnablementAndVisibility();
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
            {
                DataGridHelpers.ClearDataGrid(AudioProjectEditorDataGrid);

                var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
                audioProjectEditorDataGridService.LoadDataGrid(AudioEditorViewModel);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            _dataManager.HandleAddingData(AudioEditorViewModel);
        }

        public void SetAddRowButtonEnablement()
        {
            AudioEditorViewModel.AudioProjectEditorViewModel.ResetAddRowButtonEnablement();

            if (AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Count == 0)
                return;

            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType != NodeType.StateGroup)
            {
                if (AudioEditorViewModel.AudioSettingsViewModel.AudioFiles.Count == 0)
                    return;
            }

            var rowExistsCheckResult = CheckIfAudioProjectViewerRowExists(AudioEditorViewModel, _audioRepository, _audioEditorService);
            if (rowExistsCheckResult)
            {
                AudioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return;
            }

            var emptyCellsCheckResult = CheckIfAnyEmptyCells(AudioEditorViewModel, _audioRepository);
            if (emptyCellsCheckResult)
            {
                AudioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return;
            }
            else
            {
                AudioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;
                return;
            }
        }

        public void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
            {
                AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = true;

                if (_audioEditorService.ModdedStatesByStateGroupLookup.Count > 0)
                    AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;
                else if (_audioEditorService.ModdedStatesByStateGroupLookup.Count == 0)
                    AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = false;
        }

        private static bool CheckIfAudioProjectViewerRowExists(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioEditorService audioEditorService)
        {
            var audioProjectEditorData = DataGridHelpers.GetAudioProjectEditorDataGridRow(audioEditorViewModel, audioRepository, audioEditorService)
                .ToList();

            var rowExists = audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid
                .Any(dictionary => audioProjectEditorData.SequenceEqual(dictionary));

            return rowExists;
        }

        private static bool CheckIfAnyEmptyCells(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository)
        {
            var emptyColumns = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0]
                .Where(kvp => kvp.Value is string value && string.IsNullOrEmpty(value))
                .ToList();

            if (emptyColumns.Count > 0)
                return true;
            else
                return false;
        }

        public void SelectMovieFile()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".ca_vp8" ]);
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
