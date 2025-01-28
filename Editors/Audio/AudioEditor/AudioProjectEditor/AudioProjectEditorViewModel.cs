using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorDataGrid;
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
