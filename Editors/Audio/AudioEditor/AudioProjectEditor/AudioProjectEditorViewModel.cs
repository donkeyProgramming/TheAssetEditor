using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.Data.DataManager;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject, IEditorInterface
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;

        public string DisplayName { get; set; } = "Audio Project Editor";

        [ObservableProperty] private string _audioProjectEditorLabel = "Audio Project Editor";
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectEditorDataGrid;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (IAudioRepository audioRepository, IAudioProjectService audioProjectService, IPackFileService packFileService, IStandardDialogs standardDialogs)
        {
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

                // Clear the previous DataGrid Data
                DataGridHelpers.ClearDataGridCollection(AudioProjectEditorDataGrid);

                var parameters = new DataServiceParameters();
                parameters.AudioEditorViewModel = AudioEditorViewModel;
                parameters.AudioProjectService = _audioProjectService;
                parameters.AudioRepository = _audioRepository;
                parameters.DialogueEvent = dialogueEvent;

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
                audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            }
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorDataGridToFullDataGrid()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            HandleAddingRowData(AudioEditorViewModel, _audioProjectService, _audioRepository);
        }

        public void SetAddRowButtonEnablement()
        {
            AudioEditorViewModel.AudioProjectEditorViewModel.ResetAddRowButtonEnablement();

            if (AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Count == 0)
                return;

            if (AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType != NodeType.StateGroup)
            {
                if (AudioEditorViewModel.AudioSettingsViewModel.AudioFiles.Count == 0)
                    return;
            }

            var rowExistsCheckResult = CheckIfAudioProjectViewerRowExists(AudioEditorViewModel, _audioRepository, _audioProjectService);
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
            if (AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = true;

                if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                    AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;
                else if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count == 0)
                    AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                AudioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = false;
        }

        private static bool CheckIfAudioProjectViewerRowExists(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            var audioProjectEditorData = DataHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository, audioProjectService)
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


        public void ResetAudioProjectEditorLabel() => AudioProjectEditorLabel = $"Audio Project Editor";

        public void ResetButtonEnablement()
        {
            ResetAddRowButtonEnablement();
            ResetShowModdedStatesCheckBoxEnablement();
        }

        public void ResetAddRowButtonEnablement() => IsAddRowButtonEnabled = false;

        public void ResetShowModdedStatesCheckBoxEnablement() => IsShowModdedStatesCheckBoxEnabled = false;

        public void ResetDataGrid()
        {
            DataGridHelpers.ClearDataGridCollection(AudioProjectEditorDataGrid);
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectEditorDataGridTag));
        }

        public void Close() {}
    }
}
