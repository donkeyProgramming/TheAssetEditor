using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataManager;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public partial class AudioProjectEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Project Editor";

        [ObservableProperty] private string _audioProjectEditorLabel = "Audio Project Editor";
        [ObservableProperty] private string _audioProjectEditorDataGridTag = "AudioProjectEditorDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectEditorDataGrid;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxVisible = false;

        public AudioProjectEditorViewModel (AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioProjectService, _audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

                // Clear the previous DataGrid Data
                DataGridHelpers.ClearDataGridCollection(AudioProjectEditorDataGrid);

                var parameters = new AudioProjectDataServiceParameters();
                parameters.AudioEditorViewModel = _audioEditorViewModel;
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

            HandleAddingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        public void SetAddRowButtonEnablement()
        {
            _audioEditorViewModel.AudioProjectEditorViewModel.ResetAddRowButtonEnablement();

            if (_audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Count == 0)
                return;

            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType != NodeType.StateGroup)
            {
                if (_audioEditorViewModel.AudioSettingsViewModel.AudioFiles.Count == 0)
                    return;
            }

            var rowExistsCheckResult = CheckIfAudioProjectViewerRowExists(_audioEditorViewModel, _audioRepository, _audioProjectService);
            if (rowExistsCheckResult)
            {
                _audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return;
            }

            var emptyCellsCheckResult = CheckIfAnyEmptyCells(_audioEditorViewModel, _audioRepository);
            if (emptyCellsCheckResult)
            {
                _audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return;
            }
            else
            {
                _audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;
                return;
            }
        }

        public void SetShowModdedStatesOnlyButtonEnablementAndVisibility()
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                _audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = true;

                if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                    _audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;
                else if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count == 0)
                    _audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                _audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = false;
        }

        private static bool CheckIfAudioProjectViewerRowExists(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            var audioProjectEditorData = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository, audioProjectService)
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
