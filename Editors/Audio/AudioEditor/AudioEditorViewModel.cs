using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.UICommands;
using Shared.Core.Events;
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
        public SettingsViewModel SettingsViewModel { get; }

        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IAudioProjectUICommandFactory _audioProjectUICommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IntegrityChecker _integrityChecker;

        public string DisplayName { get; set; } = "Audio Editor";

        public AudioEditorViewModel(
            AudioProjectExplorerViewModel audioProjectExplorerViewModel,
            AudioFilesExplorerViewModel audioFilesExplorerViewModel,
            AudioProjectEditorViewModel audioProjectEditorViewModel,
            AudioProjectViewerViewModel audioProjectViewerViewModel,
            SettingsViewModel settingsViewModel,
            IUiCommandFactory uiCommandFactory,
            IAudioProjectUICommandFactory audioProjectUICommandFactory,
            IEventHub eventHub,
            IAudioEditorService audioEditorService,
            IntegrityChecker integrityChecker)
        {
            _uiCommandFactory = uiCommandFactory;
            _audioProjectUICommandFactory = audioProjectUICommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _integrityChecker = integrityChecker;

            AudioProjectExplorerViewModel = audioProjectExplorerViewModel;
            AudioFilesExplorerViewModel = audioFilesExplorerViewModel;
            AudioProjectEditorViewModel = audioProjectEditorViewModel;
            AudioProjectViewerViewModel = audioProjectViewerViewModel;
            SettingsViewModel = settingsViewModel;

            _eventHub.Publish(new InitialiseViewModelDataEvent());

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
            _audioEditorService.LoadAudioProject(_eventHub, this);
        }

        [RelayCommand] public void CompileAudioProject()
        {
            _audioEditorService.CompileAudioProject();
        }

        [RelayCommand] public void OpenAudioProjectConverter()
        {
            _uiCommandFactory.Create<OpenAudioProjectConverterWindowCommand>().Execute();
        }

        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    _uiCommandFactory.Create<CopyRowsCommand>().Execute();
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    _uiCommandFactory.Create<PasteRowsCommand>().Execute();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Delete)
            {
                foreach (var row in _audioEditorService.SelectedViewerRows)
                    _audioProjectUICommandFactory.Create(AudioProjectCommandAction.RemoveFromAudioProject, AudioProjectExplorer.NodeType.DialogueEvent).Execute(row);

                e.Handled = true;
            }
        }

        public void Close()
        {
            _eventHub.Publish(new ResetViewModelDataEvent());
            _audioEditorService.ResetAudioProject();
        }
    }
}
