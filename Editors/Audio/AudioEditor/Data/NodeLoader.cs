using Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.Data
{
    public class NodeLoader
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;
        private readonly AudioProjectEditorDataGridServiceFactory _audioProjectEditorDataGridServiceFactory;

        private readonly ILogger _logger = Logging.Create<NodeLoader>();

        public NodeLoader(
            IAudioProjectService audioProjectService,
            IAudioRepository audioRepository,
            AudioProjectEditorDataGridServiceFactory audioProjectEditorDataGridServiceFactory)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
            _audioProjectEditorDataGridServiceFactory = audioProjectEditorDataGridServiceFactory;
        }

        public void LoadNode(AudioEditorViewModel audioEditorViewModel)
        {
            ResetStuff(audioEditorViewModel);

            var selectedNodeType = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType;
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
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {soundBank.Name}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {soundBank.Name}");

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = _audioProjectService,
                SoundBank = soundBank
            };

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetDataGridService(NodeType.ActionEventSoundBank);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

            _logger.Here().Information($"Loaded Action Event SoundBank: {soundBank.Name}");
        }

        private void LoadDialogueEvent(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}");

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            _audioProjectService.BuildStateGroupsWithModdedStatesRepository(_audioProjectService.AudioProject.StateGroups, _audioProjectService.StateGroupsWithModdedStatesRepository);

            if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = _audioProjectService,
                AudioRepository = _audioRepository,
                DialogueEvent = dialogueEvent
            };

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetDataGridService(NodeType.DialogueEvent);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

            _logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
        }

        private void LoadDialogueEventSoundBank(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, _audioProjectService, soundBank.Name);

            _logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
        }

        private void LoadStateGroup(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAudioProjectEditorLabel($"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");
            audioEditorViewModel.AudioProjectViewerViewModel.SetAudioProjectViewerLabel($"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}");

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = _audioRepository,
                AudioProjectService = _audioProjectService,
                StateGroup = stateGroup
            };

            var audioProjectEditorDataGridService = _audioProjectEditorDataGridServiceFactory.GetDataGridService(NodeType.StateGroup);
            audioProjectEditorDataGridService.LoadDataGrid(audioEditorViewModel);

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

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
