using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioEditor.UICommands;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor
{
    // TODO: Resolve TOOLTIP PLACEHOLDER instances
    // TODO: Implement something where the compiler is greyed out until you have a wwise path set
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioProjectFileService _audioProjectFileService;
        private readonly IAudioProjectCompilerService _audioProjectCompilerService;
        private readonly IAudioProjectIntegrityService _audioProjectIntegrityService;

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorViewModel(
            AudioProjectExplorerViewModel audioProjectExplorerViewModel,
            AudioFilesExplorerViewModel audioFilesExplorerViewModel,
            AudioProjectEditorViewModel audioProjectEditorViewModel,
            AudioProjectViewerViewModel audioProjectViewerViewModel,
            SettingsViewModel settingsViewModel,
            IUiCommandFactory uiCommandFactory,
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IAudioProjectFileService audioProjectFileService,
            IAudioProjectCompilerService audioProjectCompilerService,
            IAudioProjectIntegrityService audioProjectIntegrityService)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorStateService = audioEditorStateService;
            _audioProjectFileService = audioProjectFileService;
            _audioProjectCompilerService = audioProjectCompilerService;
            _audioProjectIntegrityService = audioProjectIntegrityService;

            AudioProjectExplorerViewModel = audioProjectExplorerViewModel;
            AudioFilesExplorerViewModel = audioFilesExplorerViewModel;
            AudioProjectEditorViewModel = audioProjectEditorViewModel;
            AudioProjectViewerViewModel = audioProjectViewerViewModel;
            SettingsViewModel = settingsViewModel;

            _audioProjectIntegrityService.CheckDialogueEventInformationIntegrity(Wh3DialogueEventInformation.Information);
        }

        [RelayCommand] public void NewAudioProject() => _uiCommandFactory.Create<OpenNewAudioProjectWindowCommand>().Execute();

        [RelayCommand] public void SaveAudioProject()
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioProjectFileService.Save(audioProject, fileName, filePath);
        }

        [RelayCommand] public void LoadAudioProject() => _audioProjectFileService.Load();

        [RelayCommand] public void CompileAudioProject()
        {
            SaveAudioProject();

            var cleanAudioProject = AudioProject.Clean(_audioEditorStateService.AudioProject);
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioProjectCompilerService.Compile(cleanAudioProject, fileName, filePath);
        }

        [RelayCommand] public void OpenAudioProjectConverter() => _uiCommandFactory.Create<OpenAudioProjectConverterWindowCommand>().Execute();

        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    _eventHub.Publish(new CopyRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    _eventHub.Publish(new PasteRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Delete)
            {
                _uiCommandFactory.Create<RemoveViewerRowsCommand>().Execute(_audioEditorStateService.SelectedViewerRows);
                e.Handled = true;
            }
        }

        public void Close() => _audioEditorStateService.Reset();
    }
}
