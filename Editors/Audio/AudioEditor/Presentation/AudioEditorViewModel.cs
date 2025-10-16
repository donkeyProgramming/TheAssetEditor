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
using Editors.Audio.AudioEditor.Presentation.HircSettings;
using Editors.Audio.AudioEditor.Presentation.WaveformVisualiser;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace Editors.Audio.AudioEditor
{
    // TODO: Resolve TOOLTIP PLACEHOLDER instances
    // TODO: Implement something where the compiler is greyed out until you have a wwise path set
    public partial class AudioEditorViewModel(
        IUiCommandFactory uiCommandFactory,
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService,
        IAudioEditorFileService audioEditorFileService,
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
        private readonly IAudioEditorFileService _audioEditorFileService = audioEditorFileService;
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
            _audioEditorFileService.Save(audioProject, fileName, filePath);
        }

        [RelayCommand] public void LoadAudioProject() => _audioEditorFileService.LoadFromDialog();

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
