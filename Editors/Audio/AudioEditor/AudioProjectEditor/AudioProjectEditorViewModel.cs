using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly DataManager _dataManager;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;

        public string DisplayName { get; set; } = "Audio Project Editor";

        [ObservableProperty] private string _audioProjectEditorLabel;
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectEditorDataGrid;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (
            IAudioRepository audioRepository,
            IAudioEditorService audioEditorService,
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            DataManager dataManager,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory)
        {
            _audioRepository = audioRepository;
            _audioEditorService = audioEditorService;
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _dataManager = dataManager;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;

            _audioProjectEditorLabel = $"{DisplayName}";
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            var selectedNodeType = AudioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.DialogueEvent)
            {
                DataGridHelpers.ClearDataGridCollection(AudioProjectEditorDataGrid);

                var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
                audioProjectEditorDataGridService.LoadDataGrid(AudioEditorViewModel);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            _dataManager.HandleAddingRowData(AudioEditorViewModel);
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

                if (_audioEditorService.StateGroupsWithModdedStatesRepository.Count > 0)
                    AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;
                else if (_audioEditorService.StateGroupsWithModdedStatesRepository.Count == 0)
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
            AudioProjectEditorLabel = label;
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
            DataGridHelpers.ClearDataGridCollection(AudioProjectEditorDataGrid);
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectEditorDataGridTag));
        }

        public void Close() {}
    }
}
