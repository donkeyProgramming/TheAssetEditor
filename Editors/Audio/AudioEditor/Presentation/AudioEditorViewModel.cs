using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands;
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

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioEditorFileService _audioEditorFileService;
        private readonly IAudioProjectCompilerService _audioProjectCompilerService;

        [ObservableProperty] private bool _isAudioProjectLoaded = false;

        public AudioEditorViewModel(
            IUiCommandFactory uiCommandFactory,
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IAudioEditorFileService audioEditorFileService,
            IAudioProjectCompilerService audioProjectCompilerService,
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

        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            // This allows us to still paste into for example the Audio Project Editor ComboBoxes 
            if (Keyboard.FocusedElement is ComboBox comboBox && comboBox.IsEditable)
                return;

            // This is here rather than the Audio Project Viewer because the the Viewer DataGrid only recognises key presses when
            // you're focussed on the DataGrid and if you delete an item it loses focus whereas this recognises them anywhere.
            if (_audioEditorStateService.CopiedViewerRows != null && _audioEditorStateService.CopiedViewerRows.Count != 0 )
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
                {
                    _eventHub.Publish(new PasteViewerRowsShortcutActivatedEvent());
                    e.Handled = true;
                }
            }
        }

        public void Close() => _audioEditorStateService.Reset();
    }
}
