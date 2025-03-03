using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
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
        private readonly IAudioProjectService _audioProjectService;
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
            IAudioProjectService audioProjectService,
            IntegrityChecker integrityChecker)
        {
            AudioProjectExplorerViewModel = audioProjectExplorerViewModel;
            AudioProjectExplorerViewModel.AudioEditorViewModel = this;

            AudioFilesExplorerViewModel = audioFilesExplorerViewModel;
            AudioFilesExplorerViewModel.AudioEditorViewModel = this;

            AudioProjectEditorViewModel = audioProjectEditorViewModel;
            AudioProjectEditorViewModel.AudioEditorViewModel = this;

            AudioProjectViewerViewModel = audioProjectViewerViewModel;
            AudioProjectViewerViewModel.AudioEditorViewModel = this;

            AudioSettingsViewModel = audioSettingsViewModel;

            _packFileService = packFileService;
            _standardDialogs = standardDialogs;
            _audioProjectService = audioProjectService;
            _integrityChecker = integrityChecker;

            Initialise();

            _integrityChecker.CheckAudioEditorDialogueEventIntegrity(DialogueEventData);
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(this, _packFileService, _audioProjectService, _standardDialogs);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject();
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(this);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioProjectService.CompileAudioProject();
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
