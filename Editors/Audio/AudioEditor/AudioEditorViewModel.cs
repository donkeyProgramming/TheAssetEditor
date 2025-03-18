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
using NodeType = Editors.Audio.AudioEditor.AudioProjectExplorer.NodeType;
using TreeNode = Editors.Audio.AudioEditor.AudioProjectExplorer.TreeNode;

namespace Editors.Audio.AudioEditor
{
    // TODO: Refactor references to AudioEditorViewModel to in AudioEditorService so it references the individual components rather than the whole view model
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

            _audioEditorService.AudioEditorViewModel = this;

            Initialise();

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

        public TreeNode GetSelectedAudioProjectNode()
        {
            return AudioProjectExplorerViewModel.GetSelectedAudioProjectNode();
        }

        public string GetSelectedAudioProjectNodeName()
        {
            return AudioProjectExplorerViewModel.GetSelectedAudioProjectNodeName();
        }

        public NodeType GetSelectedAudioProjectNodeType()
        {
            return AudioProjectExplorerViewModel.GetSelectedAudioProjectNodeType();
        }

        public void Initialise()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = [];
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = [];
            AudioProjectViewerViewModel.SelectedDataGridRows = [];
            AudioProjectViewerViewModel.CopiedDataGridRows = [];
            AudioProjectExplorerViewModel.DialogueEventPresets = [];
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

        public void Close()
        {
            ResetAudioEditorViewModelData();
            _audioEditorService.ResetAudioProject();
        }
    }
}
