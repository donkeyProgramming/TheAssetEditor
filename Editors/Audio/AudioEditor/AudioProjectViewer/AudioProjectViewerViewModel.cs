using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioProjectViewer.CopyPasteHandler;
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
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectViewerDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
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
            SetCopyEnablement(_audioEditorViewModel);
        }

        private void SetSelectedDataGridRows(IList selectedItems)
        {
            SelectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, object>>())
                SelectedDataGridRows.Add(item);
        }

        partial void OnAudioProjectViewerDataGridChanged(ObservableCollection<Dictionary<string, object>> value)
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
                SetPasteEnablement(_audioEditorViewModel, _audioRepository, _audioProjectService);
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
                CopyDialogueEventRows(_audioEditorViewModel, _audioRepository, _audioProjectService);
        }

        [RelayCommand]  public void PasteRows()
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioProjectService, _audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
                PasteDialogueEventRows(_audioEditorViewModel, _audioRepository, _audioProjectService, dialogueEvent);
            }
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
