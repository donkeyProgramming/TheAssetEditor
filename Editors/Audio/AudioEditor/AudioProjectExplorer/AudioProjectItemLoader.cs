using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class AudioProjectItemLoader
    {
        private static readonly ILogger s_logger = Logging.Create<AudioProjectItemLoader>();

        public static void HandleSelectedTreeViewItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            ResetStuff(audioEditorViewModel);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                LoadActionEventSoundBank(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEventSoundBank)
                LoadDialogueEventSoundBank(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                LoadDialogueEvent(audioEditorViewModel, audioProjectService, audioRepository);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                LoadStateGroup(audioEditorViewModel, audioProjectService, audioRepository);

            SetStuff(audioEditorViewModel);
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

        private static void LoadActionEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {soundBank.Name}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {soundBank.Name}";

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            var parameters = new AudioProjectDataServiceParameters
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

        private static void LoadDialogueEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DialogueEventFilter.HandleDialogueEventsPresetFilter(audioEditorViewModel.AudioProjectExplorerViewModel, audioProjectService, soundBank.Name);
        }

        private static void LoadDialogueEvent(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AudioProjectHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AudioProjectHelpers.AddExtraUnderscoresToString(dialogueEvent.Name)}";

            audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsEnablementAndVisibility();

            // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
            audioProjectService.BuildStateGroupsWithModdedStatesRepository(audioProjectService.AudioProject.StateGroups, audioProjectService.StateGroupsWithModdedStatesRepository);

            if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var parameters = new AudioProjectDataServiceParameters
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

            s_logger.Here().Information($"Loaded DialogueEvent: {dialogueEvent.Name}");
        }

        private static void LoadStateGroup(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AudioProjectHelpers.AddExtraUnderscoresToString(stateGroup.Name)}";
            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AudioProjectHelpers.AddExtraUnderscoresToString(stateGroup.Name)}";

            var parameters = new AudioProjectDataServiceParameters
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

            s_logger.Here().Information($"Loaded StateGroup: {stateGroup.Name}");
        }
    }
}
