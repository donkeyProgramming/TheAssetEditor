using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.UICommands;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor
{
    // TODO: Resolve TOOLTIP PLACEHOLDER instances
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; }
        public AudioSettingsViewModel AudioSettingsViewModel { get; }

        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IntegrityChecker _integrityChecker;

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorViewModel(
            IUiCommandFactory uiCommandFactory,
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
            _uiCommandFactory = uiCommandFactory;
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
            _uiCommandFactory.Create<OpenNewAudioProjectWindowCommand>().Execute();
        }

        [RelayCommand] public void SaveAudioProject()
        {
            var audioProject = AudioProject.GetAudioProject(_audioEditorService.AudioProject);
            _audioEditorService.SaveAudioProject(audioProject, audioProject.FileName, audioProject.DirectoryPath);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioEditorService.LoadAudioProject(this);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioEditorService.CompileAudioProject();
        }

        [RelayCommand] public void OpenAudioProjectConverter()
        {
            _uiCommandFactory.Create<OpenAudioProjectConverterWindowCommand>().Execute();
        }

        public void InitialiseAudioEditorData()
        {
            AudioProjectEditorViewModel.AudioProjectEditorDataGrid = new();
            AudioProjectViewerViewModel.AudioProjectViewerDataGrid = new();
            AudioProjectViewerViewModel._selectedDataGridRows = [];
            AudioProjectViewerViewModel._copiedRows = [];
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
            AudioProjectViewerViewModel._selectedDataGridRows = null;
            AudioProjectViewerViewModel._copiedRows = null;
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
