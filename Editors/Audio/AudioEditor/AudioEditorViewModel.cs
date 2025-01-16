using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.AudioEditorMenu;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettingsEditor;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public AudioEditorMenuViewModel AudioEditorMenuViewModel { get; set; }
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; set; }
        public AudioSettingsEditorViewModel AudioSettingsViewModel { get; set; }

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorViewModel(IAudioRepository audioRepository, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            AudioEditorMenuViewModel = new AudioEditorMenuViewModel(this, _audioRepository, _packFileService, _audioProjectService, _packFileUiProvider);
            AudioProjectExplorerViewModel = new AudioProjectExplorerViewModel(this, _audioRepository, _audioProjectService);
            AudioFilesExplorerViewModel = new AudioFilesExplorerViewModel(this, _packFileService);
            AudioProjectEditorViewModel = new AudioProjectEditorViewModel(this, _audioRepository, _audioProjectService);
            AudioProjectViewerViewModel = new AudioProjectViewerViewModel(this, _audioRepository, _audioProjectService);
            AudioSettingsViewModel = new AudioSettingsEditorViewModel();

            Initialise();

            IntegrityChecker.CheckAudioEditorDialogueEventIntegrity(_audioRepository, DialogueEventData);
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid = null;
            AudioProjectViewerViewModel.AudioProjectEditorFullDataGrid = null;
            AudioProjectViewerViewModel.SelectedDataGridRows = null;
            AudioProjectViewerViewModel.CopiedDataGridRows = null;
            AudioProjectExplorerViewModel._selectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel._previousSelectedAudioProjectTreeItem = null;
            AudioProjectExplorerViewModel.AudioProjectTreeViewItems.Clear();
            AudioProjectExplorerViewModel.DialogueEventSoundBankFiltering.Clear();
        }

        public void Initialise()
        {
            AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid = [];
            AudioProjectViewerViewModel.AudioProjectEditorFullDataGrid = [];
            AudioProjectViewerViewModel.SelectedDataGridRows = [];
            AudioProjectViewerViewModel.CopiedDataGridRows = [];
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
