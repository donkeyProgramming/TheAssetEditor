using Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.Data
{
    public class NodeLoader
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly AudioProjectViewerDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<NodeLoader>();

        public NodeLoader(
            IAudioProjectService audioProjectService,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory,
            AudioProjectViewerDataGridServiceFactory audioProjectViewerDataGridServiceFactory)
        {
            _audioProjectService = audioProjectService;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;
            _audioProjectViewerDataGridServiceFactory = audioProjectViewerDataGridServiceFactory;
        }

        public void LoadNode(AudioEditorViewModel audioEditorViewModel)
        {
            ResetStuff(audioEditorViewModel);

            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.ActionEventSoundBank)
                LoadActionEventSoundBank(audioEditorViewModel);
            else if (selectedNodeType == NodeType.DialogueEventSoundBank)
                LoadDialogueEventSoundBank(audioEditorViewModel);
            else if (selectedNodeType == NodeType.DialogueEvent)
                LoadDialogueEvent(audioEditorViewModel);
            else if (selectedNodeType == NodeType.StateGroup)
                LoadStateGroup(audioEditorViewModel);

            SetStuff(audioEditorViewModel);
        }

        private void LoadActionEventSoundBank(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {soundBank.Name}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {soundBank.Name}");

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.ActionEventSoundBank);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectviewerDataGridService = _audioProjectViewerDataGridServiceFactory.GetService(NodeType.ActionEventSoundBank);
            audioProjectviewerDataGridService.LoadDataGrid(audioEditorViewModel);

            _logger.Here().Information($"Loaded Action Event SoundBank: {soundBank.Name}");
        }

        private void LoadDialogueEvent(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            _audioProjectService.BuildStateGroupsWithModdedStatesRepository(_audioProjectService.AudioProject.StateGroups, _audioProjectService.StateGroupsWithModdedStatesRepository);

            if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectviewerDataGridService = _audioProjectViewerDataGridServiceFactory.GetService(NodeType.DialogueEvent);
            audioProjectviewerDataGridService.LoadDataGrid(audioEditorViewModel);

            _logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
        }

        private void LoadDialogueEventSoundBank(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, _audioProjectService, soundBank.Name);

            _logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
        }

        private void LoadStateGroup(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.StateGroup);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectViewerDataGridService = _audioProjectViewerDataGridServiceFactory.GetService(NodeType.StateGroup);
            audioProjectViewerDataGridService.LoadDataGrid(audioEditorViewModel);

            _logger.Here().Information($"Loaded State Group: {stateGroup.Name}");
        }

        private static void ResetStuff(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectExplorerViewModel.ResetButtonEnablement();

            audioEditorViewModel.AudioFilesExplorerViewModel.ResetButtonEnablement();

            audioEditorViewModel.AudioProjectEditorViewModel.ResetAudioProjectEditorLabel();
            audioEditorViewModel.AudioProjectEditorViewModel.ResetButtonEnablement();
            audioEditorViewModel.AudioProjectEditorViewModel.ResetDataGrid();

            audioEditorViewModel.AudioProjectViewerViewModel.ResetAudioProjectViewerLabel();
            audioEditorViewModel.AudioProjectViewerViewModel.ResetButtonEnablement();
            audioEditorViewModel.AudioProjectViewerViewModel.ResetDataGrid();

            audioEditorViewModel.AudioSettingsViewModel.ResetAudioSettingsView();
        }

        private static void SetStuff(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioFilesExplorerViewModel.SetButtonEnablement();

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
            audioEditorViewModel.AudioProjectEditorViewModel.SetShowModdedStatesOnlyButtonEnablementAndVisibility();

            audioEditorViewModel.AudioProjectViewerViewModel.SetCopyEnablement();
            audioEditorViewModel.AudioProjectViewerViewModel.SetPasteEnablement();
        }
    }
}
