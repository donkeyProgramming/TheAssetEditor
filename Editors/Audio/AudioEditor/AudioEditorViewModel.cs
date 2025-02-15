using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.AudioEditorMenu;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;
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
        private readonly ApplicationSettingsService _applicationSettingsService;

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorMenuViewModel AudioEditorMenuViewModel { get; set; }
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; set; }
        public AudioSettingsViewModel AudioSettingsViewModel { get; set; }

        public AudioEditorViewModel(
            IAudioRepository audioRepository,
            IPackFileService packFileService,
            IAudioProjectService audioProjectService,
            IStandardDialogs packFileUiProvider,
            ApplicationSettingsService applicationSettingsService)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;
            _applicationSettingsService = applicationSettingsService;

            AudioEditorMenuViewModel = new AudioEditorMenuViewModel(this, _audioRepository, _packFileService, _audioProjectService, _packFileUiProvider, _applicationSettingsService);
            AudioProjectExplorerViewModel = new AudioProjectExplorerViewModel(this, _audioRepository, _audioProjectService);
            AudioFilesExplorerViewModel = new AudioFilesExplorerViewModel(this, _packFileService, _audioRepository, _audioProjectService);
            AudioProjectEditorViewModel = new AudioProjectEditorViewModel(this, _audioRepository, _audioProjectService);
            AudioProjectViewerViewModel = new AudioProjectViewerViewModel(this, _audioRepository, _audioProjectService);
            AudioSettingsViewModel = new AudioSettingsViewModel(this, _audioProjectService);

            Initialise();

            IntegrityChecker.CheckAudioEditorDialogueEventIntegrity(_audioRepository, DialogueEventData);
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = null;
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = null;
            AudioProjectViewerViewModel.SelectedDataGridRows = null;
            AudioProjectViewerViewModel.CopiedDataGridRows = null;
            AudioProjectExplorerViewModel._selectedAudioProjectTreeNode = null;
            AudioProjectExplorerViewModel.AudioProjectTree.Clear();
        }

        public void Initialise()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = [];
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = [];
            AudioProjectViewerViewModel.SelectedDataGridRows = [];
            AudioProjectViewerViewModel.CopiedDataGridRows = [];
            AudioProjectExplorerViewModel.DialogueEventPresets = [];
        }

        public void Close()
        {
            ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
        }
    }
}
