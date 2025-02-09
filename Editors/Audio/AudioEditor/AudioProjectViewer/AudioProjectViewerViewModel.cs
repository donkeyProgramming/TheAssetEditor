using System.Collections;
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

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Project Viewer";

        [ObservableProperty] private string _audioProjectViewerLabel = "Audio Project Viewer";
        [ObservableProperty] private string _audioProjectViewerDataGridTag = "AudioProjectViewerDataGrid";
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _audioProjectViewerDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, string>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        public AudioProjectViewerViewModel(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            SetSelectedDataGridRows(selectedItems);
            SetButtonEnablement();
            SetCopyEnablement();
        }

        private void SetSelectedDataGridRows(IList selectedItems)
        {
            SelectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, string>>())
                SelectedDataGridRows.Add(item);
        }

        public void SetCopyEnablement()
        {
            if (_audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows != null)
                _audioEditorViewModel.AudioProjectViewerViewModel.IsCopyEnabled = _audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.Any();
        }

        partial void OnAudioProjectViewerDataGridChanged(ObservableCollection<Dictionary<string, string>> value)
        {
            if (AudioProjectViewerDataGrid != null)
            {
                AudioProjectViewerDataGrid.CollectionChanged += AudioProjectViewerDataGrid_CollectionChanged;
                OnAudioProjectViewerDataGridChanged();
            }
        }

        private void AudioProjectViewerDataGrid_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnAudioProjectViewerDataGridChanged();
        }

        private void OnAudioProjectViewerDataGridChanged()
        {
            if (AudioProjectViewerDataGrid != null && AudioProjectViewerDataGrid.Count > 0)
                SetPasteEnablement();
        }

        public void SetPasteEnablement()
        {
            if (!_audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Any())
            {
                _audioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = false;
                return;
            }

            var areAnyCopiedRowsInDataGrid = _audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows
                .Any(copiedRow => _audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid
                .Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioProjectService, _audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
                var dialogueEventStateGroups = _audioRepository
                    .DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name]
                    .Select(kvp => AudioProjectHelpers.AddExtraUnderscoresToString(kvp.Key))
                    .ToList();

                var copiedDataGridRowStateGroups = _audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows[0]
                    .Select(kvp => kvp.Key)
                    .ToList();

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                _audioEditorViewModel.AudioProjectViewerViewModel.IsPasteEnabled = areStateGroupsEqual && !areAnyCopiedRowsInDataGrid;
            }
        }

        [RelayCommand] public void UpdateAudioProjectViewerDataGridRow()
        {
            HandleUpdatingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void RemoveAudioProjectViewerDataGridRow()
        {
            HandleRemovingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void CopyRows()
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                CopyDialogueEventRows();
        }

        public void CopyDialogueEventRows()
        {
            _audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows = [];

            foreach (var item in _audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows)
                _audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows.Add(new Dictionary<string, string>(item));

            SetPasteEnablement();
        }

        [RelayCommand]  public void PasteRows()
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                PasteDialogueEventRows();
        }

        public void PasteDialogueEventRows()
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioProjectService, _audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            foreach (var copiedDataGridRow in _audioEditorViewModel.AudioProjectViewerViewModel.CopiedDataGridRows)
            {
                _audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(copiedDataGridRow);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = _audioEditorViewModel,
                    AudioProjectEditorRow = copiedDataGridRow,
                    AudioRepository = _audioRepository,
                    DialogueEvent = dialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
            }

            SetPasteEnablement();
        }

        public void SetButtonEnablement()
        {
            ResetButtonEnablement();

            if (SelectedDataGridRows.Count == 1)
            {
                IsUpdateRowButtonEnabled = true;
                IsRemoveRowButtonEnabled = true;
            }
            else if (SelectedDataGridRows.Count > 1)
                IsRemoveRowButtonEnabled = true;
        }

        public void ResetAudioProjectViewerLabel() => AudioProjectViewerLabel = $"Audio Project Viewer";
        
        public void ResetButtonEnablement()
        {
            IsUpdateRowButtonEnabled = false;
            IsRemoveRowButtonEnabled = false;
        }

        public void ResetDataGrid()
        {
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(AudioProjectViewerDataGridTag));
            DataGridHelpers.ClearDataGridCollection(AudioProjectViewerDataGrid);
        }

        public void Close() { }
    }
}
