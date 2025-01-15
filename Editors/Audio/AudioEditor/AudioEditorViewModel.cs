using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioSettingsEditor;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.AudioEditor.NewAudioProject;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.CopyPasteHandler;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        public AudioSettingsEditorViewModel AudioSettingsViewModel { get; set; }

        public string DisplayName { get; set; } = "Audio Editor";

        // Audio Editor data
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorFullDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;

        // Audio Project Editor
        [ObservableProperty] private string _audioProjectViewerLabel = "Audio Project Viewer";
        [ObservableProperty] private string _audioProjectEditorFullDataGridTag = "AudioProjectEditorFullDataGrid";
        [ObservableProperty] private bool _isCopyEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = false;

        public AudioEditorViewModel(IAudioRepository audioRepository, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            AudioProjectExplorerViewModel = new AudioProjectExplorerViewModel(this, _audioRepository, _audioProjectService);
            AudioFilesExplorerViewModel = new AudioFilesExplorerViewModel(this, _packFileService);
            AudioProjectEditorViewModel = new AudioProjectEditorViewModel(this, _audioRepository, _audioProjectService);
            AudioSettingsViewModel = new AudioSettingsEditorViewModel();


            Initialise();

            CheckAudioEditorDialogueEventIntegrity(_audioRepository, DialogueEventData);
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            SetButtonEnablement(this, _audioProjectService, selectedItems);
            SetIsCopyEnabled(this);
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
                SetIsPasteEnabled(this, _audioRepository, _audioProjectService);
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_packFileService, this, _audioProjectService, _packFileUiProvider);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject(_packFileService);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(_packFileService, _audioRepository, this, _packFileUiProvider);
        }



        [RelayCommand] public void CopyRows()
        {
            if (AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent)
                CopyDialogueEventRows(this, _audioRepository, _audioProjectService);
        }

        [RelayCommand] public void PasteRows()
        {
            if (IsPasteEnabled && AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                PasteDialogueEventRows(this, _audioRepository, _audioProjectService, selectedDialogueEvent);
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid = null;
            AudioProjectEditorFullDataGrid = null;
            SelectedDataGridRows = null;
            CopiedDataGridRows = null;
            AudioProjectExplorerViewModel._selectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel._previousSelectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel.AudioProjectTreeViewItems.Clear();
            AudioProjectExplorerViewModel.DialogueEventSoundBankFiltering.Clear();
        }

        public void Initialise()
        {
            AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid = [];
            AudioProjectEditorFullDataGrid = [];
            SelectedDataGridRows = [];
            CopiedDataGridRows = [];
            AudioProjectExplorerViewModel.DialogueEventPresets = [];
            AudioProjectExplorerViewModel.AudioProjectTreeViewItems = _audioProjectService.AudioProject.AudioProjectTreeViewItems;
        }

        public void Close()
        {
            ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
        }
    }
}
