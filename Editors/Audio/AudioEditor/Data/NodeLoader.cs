using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid;
using Editors.Audio.AudioEditor.DataGrids;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.Data
{
    public class NodeLoader
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;
        private readonly AudioProjectViewerDataGridServiceFactory _audioProjectViewerDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<NodeLoader>();

        public NodeLoader(
            IAudioEditorService audioEditorService,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory,
            AudioProjectViewerDataGridServiceFactory audioProjectViewerDataGridServiceFactory)
        {
            _audioEditorService = audioEditorService;
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
            var soundBank = DataHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

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
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataGridHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataGridHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            _audioEditorService.BuildStateGroupsWithModdedStatesRepository(_audioEditorService.AudioProject.StateGroups, _audioEditorService.StateGroupsWithModdedStatesRepository);

            if (_audioEditorService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetService(NodeType.DialogueEvent);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectviewerDataGridService = _audioProjectViewerDataGridServiceFactory.GetService(NodeType.DialogueEvent);
            audioProjectviewerDataGridService.LoadDataGrid(audioEditorViewModel);

            _logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
        }

        private void LoadDialogueEventSoundBank(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, _audioEditorService, soundBank.Name);

            _logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
        }

        private void LoadStateGroup(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");

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
