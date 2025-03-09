using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid;
using Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid;
using Editors.Audio.AudioEditor.DataGrids;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
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

        public void HandleLoadingNode(AudioEditorViewModel audioEditorViewModel)
        {
            ResetStuff(audioEditorViewModel);

            var selectedNodeType = audioEditorViewModel.GetSelectedAudioProjectNodeType();
            if (selectedNodeType == NodeType.ActionEventSoundBank)
                LoadActionEventSoundBankNode(audioEditorViewModel);
            else if (selectedNodeType == NodeType.DialogueEventSoundBank)
                LoadDialogueEventSoundBankNode(audioEditorViewModel);
            else if (selectedNodeType == NodeType.DialogueEvent)
                LoadDialogueEventNode(audioEditorViewModel);
            else if (selectedNodeType == NodeType.StateGroup)
                LoadStateGroupNode(audioEditorViewModel);

            SetStuff(audioEditorViewModel);
        }

        private void LoadActionEventSoundBankNode(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {soundBank.Name}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {soundBank.Name}");

            LoadDataGrids(audioEditorViewModel, NodeType.ActionEventSoundBank);

            _logger.Here().Information($"Loaded Action Event SoundBank: {soundBank.Name}");
        }

        private void LoadDialogueEventSoundBankNode(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, _audioEditorService, soundBank.Name);

            _logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
        }

        private void LoadDialogueEventNode(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataGridHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataGridHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            _audioEditorService.BuildModdedStatesByStateGroupLookup(_audioEditorService.AudioProject.StateGroups, _audioEditorService.ModdedStatesByStateGroupLookup);

            if (_audioEditorService.ModdedStatesByStateGroupLookup.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            LoadDataGrids(audioEditorViewModel, NodeType.DialogueEvent);

            _logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
        }

        private void LoadStateGroupNode(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");

            LoadDataGrids(audioEditorViewModel, NodeType.StateGroup);

            _logger.Here().Information($"Loaded State Group: {stateGroup.Name}");
        }

        private void LoadDataGrids(AudioEditorViewModel audioEditorViewModek, NodeType nodeType)
        {
            var editorService = _audioProjectEditorDataGridServiceFactory.GetService(nodeType);
            editorService.LoadDataGrid(audioEditorViewModek);

            var viewerService = _audioProjectViewerDataGridServiceFactory.GetService(nodeType);
            viewerService.LoadDataGrid(audioEditorViewModek);
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

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();
        }
    }
}
