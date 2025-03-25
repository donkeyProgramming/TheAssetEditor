using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.NewAudioProject;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; }
        public AudioSettingsViewModel AudioSettingsViewModel { get; }

        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IntegrityChecker _integrityChecker;

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorViewModel(
            AudioProjectExplorerViewModel audioProjectExplorerViewModel,
            AudioFilesExplorerViewModel audioFilesExplorerViewModel,
            AudioProjectEditorViewModel audioProjectEditorViewModel,
            AudioProjectViewerViewModel audioProjectViewerViewModel,
            AudioSettingsViewModel audioSettingsViewModel,
            IPackFileService packFileService,
            IStandardDialogs standardDialogs,
            IAudioEditorService audioEditorService,
            IntegrityChecker integrityChecker)
        {
            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _audioEditorService = audioEditorService;
            _integrityChecker = integrityChecker;

            AudioProjectExplorerViewModel = audioProjectExplorerViewModel;
            AudioFilesExplorerViewModel = audioFilesExplorerViewModel;
            AudioProjectEditorViewModel = audioProjectEditorViewModel;
            AudioProjectViewerViewModel = audioProjectViewerViewModel;
            AudioSettingsViewModel = audioSettingsViewModel;

            InitialiseAudioEditorData();

            InitialiseAudioEditorService();

            _integrityChecker.CheckDialogueEventIntegrity(DialogueEventData);
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_packFileService, _audioEditorService, _standardDialogs);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioEditorService.SaveAudioProject();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioEditorService.LoadAudioProject(this);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioEditorService.CompileAudioProject();
        }

        public void InitialiseAudioEditorData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = [];
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = [];
            AudioProjectViewerViewModel.SelectedDataGridRows = [];
            AudioProjectViewerViewModel.CopiedDataGridRows = [];
            AudioProjectExplorerViewModel.DialogueEventPresets = [];
        }
        private void InitialiseAudioEditorService()
        {
            _audioEditorService.AudioEditorViewModel = this;
            _audioEditorService.AudioProjectExplorerViewModel = AudioProjectExplorerViewModel;
            _audioEditorService.AudioFilesExplorerViewModel = AudioFilesExplorerViewModel;
            _audioEditorService.AudioProjectEditorViewModel = AudioProjectEditorViewModel;
            _audioEditorService.AudioProjectViewerViewModel = AudioProjectViewerViewModel;
            _audioEditorService.AudioSettingsViewModel = AudioSettingsViewModel;
        }
        public void ResetAudioEditorData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = null;
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = null;
            AudioProjectViewerViewModel.SelectedDataGridRows = null;
            AudioProjectViewerViewModel.CopiedDataGridRows = null;
            AudioProjectExplorerViewModel._selectedAudioProjectTreeNode = null;
            AudioProjectExplorerViewModel.AudioProjectTree.Clear();
        }

        public void Close()
        {
            ResetAudioEditorData();
            _audioEditorService.ResetAudioProject();
        }
    }
}
