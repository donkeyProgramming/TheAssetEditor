using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands.Dialogs;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer;
using Editors.Audio.AudioEditor.Presentation.Settings;
using Editors.Audio.AudioEditor.Presentation.WaveformVisualiser;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor.Presentation
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioEditorFileService _audioEditorFileService;
        private readonly IAudioProjectCompilerService _audioProjectCompilerService;
        private readonly IAudioEditorIntegrityService _audioEditorIntegrityService;
        private readonly IShortcutService _shortcutService;

        [ObservableProperty] private bool _isAudioProjectLoaded = false;

        public AudioEditorViewModel(
            IUiCommandFactory uiCommandFactory,
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IAudioEditorFileService audioEditorFileService,
            IAudioProjectCompilerService audioProjectCompilerService,
            IAudioEditorIntegrityService audioEditorIntegrityService,
            IShortcutService shortcutService,
            AudioProjectExplorerViewModel audioProjectExplorerViewModel,
            AudioFilesExplorerViewModel audioFilesExplorerViewModel,
            AudioProjectEditorViewModel audioProjectEditorViewModel,
            AudioProjectViewerViewModel audioProjectViewerViewModel,
            SettingsViewModel settingsViewModel,
            WaveformVisualiserViewModel waveformVisualiserViewModel)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorStateService = audioEditorStateService;
            _audioEditorFileService = audioEditorFileService;
            _audioProjectCompilerService = audioProjectCompilerService;
            _audioEditorIntegrityService = audioEditorIntegrityService;
            _shortcutService = shortcutService;

            AudioProjectExplorerViewModel = audioProjectExplorerViewModel;
            AudioFilesExplorerViewModel = audioFilesExplorerViewModel;
            AudioProjectEditorViewModel = audioProjectEditorViewModel;
            AudioProjectViewerViewModel = audioProjectViewerViewModel;
            SettingsViewModel = settingsViewModel;
            WaveformVisualiserViewModel = waveformVisualiserViewModel;

            _eventHub.Register<AudioProjectLoadedEvent>(this, OnAudioProjectLoaded);
        }

        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public WaveformVisualiserViewModel WaveformVisualiserViewModel { get; }

        public string DisplayName { get; set; } = "Audio Editor";

        private void OnAudioProjectLoaded(AudioProjectLoadedEvent e) => IsAudioProjectLoaded = true;

        [RelayCommand] public void NewAudioProject() => _uiCommandFactory.Create<OpenNewAudioProjectWindowCommand>().Execute();

        [RelayCommand] public void SaveAudioProject()
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioEditorFileService.Save(audioProject, fileName, filePath);
        }

        [RelayCommand] public void LoadAudioProject() => _audioEditorFileService.LoadFromDialog();

        [RelayCommand] public void CompileAudioProject()
        {
            SaveAudioProject();
            var cleanAudioProject = AudioProjectFile.Clean(_audioEditorStateService.AudioProject);
            var fileName = _audioEditorStateService.AudioProjectFileName;
            var filePath = _audioEditorStateService.AudioProjectFilePath;
            _audioProjectCompilerService.Compile(cleanAudioProject, fileName, filePath);
        }

        [RelayCommand] public void OpenDialogueEventMerger() => _uiCommandFactory.Create<OpenDialogueEventMergerWindowCommand>().Execute();

        [RelayCommand] public void OpenAudioProjectMerger() => _uiCommandFactory.Create<OpenAudioProjectMergerWindowCommand>().Execute();

        [RelayCommand] public void OpenAudioProjectConverter() => _uiCommandFactory.Create<OpenAudioProjectConverterWindowCommand>().Execute();

        [RelayCommand] public void RefreshSourceIds()
        {
            _audioEditorIntegrityService.RefreshSourceIds(_audioEditorStateService.AudioProject);
            SaveAudioProject();
        }

        public void OnPreviewKeyDown(KeyEventArgs e, bool isTextInputFocussed, bool isSettingsAudioFilesListViewFocussed)
        {
            _shortcutService.HandleShortcut(e, isTextInputFocussed, isSettingsAudioFilesListViewFocussed);
        }

        public void Close() => _audioEditorStateService.Reset();
    }
}
