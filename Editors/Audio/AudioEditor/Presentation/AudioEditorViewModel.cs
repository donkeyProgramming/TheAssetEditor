using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.HircSettings;
using Editors.Audio.AudioEditor.Presentation.WaveformVisualiser;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.Events;
using Shared.Core.ToolCreation;
using Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Core;

namespace Editors.Audio.AudioEditor
{
    // TODO: Resolve TOOLTIP PLACEHOLDER instances
    // TODO: Implement something where the compiler is greyed out until you have a wwise path set
    public partial class AudioEditorViewModel(
        IUiCommandFactory uiCommandFactory,
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService,
        IAudioProjectFileService audioProjectFileService,
        IAudioProjectCompilerService audioProjectCompilerService,
        AudioProjectExplorerViewModel audioProjectExplorerViewModel,
        AudioFilesExplorerViewModel audioFilesExplorerViewModel,
        AudioProjectEditorViewModel audioProjectEditorViewModel,
        AudioProjectViewerViewModel audioProjectViewerViewModel,
        HircSettingsViewModel settingsViewModel,
        WaveformVisualiserViewModel waveformVisualiserViewModel) : ObservableObject, IEditorInterface
    {
        private readonly IUiCommandFactory _uiCommandFactory = uiCommandFactory;
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly IAudioProjectCompilerService _audioProjectCompilerService = audioProjectCompilerService;

        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; } = audioProjectExplorerViewModel;
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; } = audioFilesExplorerViewModel;
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; } = audioProjectEditorViewModel;
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; } = audioProjectViewerViewModel;
        public HircSettingsViewModel SettingsViewModel { get; } = settingsViewModel;
        public WaveformVisualiserViewModel WaveformVisualiserViewModel { get; } = waveformVisualiserViewModel;

        public string DisplayName { get; set; } = "Audio Editor";

        [RelayCommand] public void NewAudioProject() => _uiCommandFactory.Create<OpenNewAudioProjectWindowCommand>().Execute();

        [RelayCommand] public void SaveAudioProject()
        {
            if (_audioEditorStateService.AudioProject == null)
                return;

            var audioProject = _audioEditorStateService.AudioProject;
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioProjectFileService.Save(audioProject, fileName, filePath);
        }

        [RelayCommand] public void LoadAudioProject() => _audioProjectFileService.LoadFromDialog();

        [RelayCommand] public void CompileAudioProject()
        {
            if (_audioEditorStateService.AudioProject == null)
                return;

            SaveAudioProject();

            var cleanAudioProject = AudioProjectFile.Clean(_audioEditorStateService.AudioProject);
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioProjectCompilerService.Compile(cleanAudioProject, fileName, filePath);

            _eventHub.Publish(new AudioProjectInitialisedEvent());
        }

        [RelayCommand] public void OpenDialogueEventMerger() => _uiCommandFactory.Create<OpenDialogueEventMergerWindowCommand>().Execute();

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
                if (_audioEditorStateService.SelectedViewerRows != null && _audioEditorStateService.SelectedViewerRows.Count > 0)
                    _uiCommandFactory.Create<RemoveViewerRowsCommand>().Execute(_audioEditorStateService.SelectedViewerRows);

                e.Handled = true;
            }
        }

        public void Close() => _audioEditorStateService.Reset();
    }
}
