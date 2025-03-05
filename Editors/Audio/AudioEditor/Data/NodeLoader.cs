using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.Data
{
    public static class NodeLoader
    {
        private static readonly ILogger s_logger = Logging.Create<DataManager>();

        public static void HandleLoadingSelectedAudioProjectItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            ResetStuff(audioEditorViewModel);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                LoadActionEventSoundBank(audioEditorViewModel, audioProjectService);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEventSoundBank)
                LoadDialogueEventSoundBank(audioEditorViewModel, audioProjectService);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                LoadDialogueEvent(audioEditorViewModel, audioProjectService, audioRepository);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                LoadStateGroup(audioEditorViewModel, audioProjectService, audioRepository);

            SetStuff(audioEditorViewModel);
        }

        private static void LoadActionEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {soundBank.Name}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {soundBank.Name}";

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = audioProjectService,
                SoundBank = soundBank
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
            audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

            s_logger.Here().Information($"Loaded Action Event SoundBank: {soundBank.Name}");
        }

        private static void LoadDialogueEvent(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}";

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            audioProjectService.BuildStateGroupsWithModdedStatesRepository(audioProjectService.AudioProject.StateGroups, audioProjectService.StateGroupsWithModdedStatesRepository);

            if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = audioProjectService,
                AudioRepository = audioRepository,
                DialogueEvent = dialogueEvent
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

            s_logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
        }

        private static void LoadDialogueEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, audioProjectService, soundBank.Name);

            s_logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
        }

        private static void LoadStateGroup(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)}";

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = audioRepository,
                AudioProjectService = audioProjectService,
                StateGroup = stateGroup
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
            audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
            audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

            s_logger.Here().Information($"Loaded State Group: {stateGroup.Name}");
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
