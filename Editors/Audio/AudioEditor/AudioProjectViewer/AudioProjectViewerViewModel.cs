using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioProjectEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.AudioProjectViewer.CopyPasteHandler;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataManager;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerViewModel : ObservableObject, IEditorInterface
    {
        public string DisplayName { get; set; } = "Audio Project Viewer";

        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorFullDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] private string _audioProjectViewerLabel = "Audio Project Viewer";
        [ObservableProperty] private string _audioProjectEditorFullDataGridTag = "AudioProjectEditorFullDataGrid";
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
            SetButtonEnablement(_audioEditorViewModel, _audioProjectService, selectedItems);
            SetIsCopyEnabled(_audioEditorViewModel);
        }

        partial void OnAudioProjectEditorFullDataGridChanged(ObservableCollection<Dictionary<string, object>> value)
        {
            if (AudioProjectEditorFullDataGrid != null)
            {
                AudioProjectEditorFullDataGrid.CollectionChanged += AudioProjectEditorFullDataGrid_CollectionChanged;
                OnAudioProjectEditorFullDataGridChanged();
            }
        }

        private void AudioProjectEditorFullDataGrid_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnAudioProjectEditorFullDataGridChanged();
        }

        private void OnAudioProjectEditorFullDataGridChanged()
        {
            if (AudioProjectEditorFullDataGrid != null && AudioProjectEditorFullDataGrid.Count > 0)
                SetIsPasteEnabled(_audioEditorViewModel, _audioRepository, _audioProjectService);
        }

        [RelayCommand] public void CopyRows()
        {
            if (_audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent)
                CopyDialogueEventRows(_audioEditorViewModel, _audioRepository, _audioProjectService);
        }

        [RelayCommand]  public void PasteRows()
        {
            if (IsPasteEnabled && _audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                PasteDialogueEventRows(_audioEditorViewModel, _audioRepository, _audioProjectService, selectedDialogueEvent);
        }

        public void RemoveAudioProjectEditorFullDataGridRow()
        {
            HandleRemovingRowData(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        public void Close() { }
    }
}
