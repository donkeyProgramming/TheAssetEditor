using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectDataService;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class AudioProjectDataManager
    {
        private static readonly ILogger s_logger = Logging.Create<AudioProjectDataManager>();

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

            s_logger.Here().Information($"Loaded Dialogue Event SoundBank: {soundBank.Name}");
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

            s_logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name}");
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

            s_logger.Here().Information($"Loaded State Group: {stateGroup.Name}");
        }

        public static void HandleAddingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var audioProjectEditorRow = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository, audioProjectService);
            AddAudioProjectEditorDataGridDataToAudioProjectViewer(audioEditorViewModel, audioProjectEditorRow);

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                AddActionEventSoundBankData(audioEditorViewModel, audioProjectService, audioProjectEditorRow);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                AddDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository, audioProjectEditorRow);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                AddStateGroupData(audioEditorViewModel, audioProjectService, audioProjectEditorRow);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private static void AddAudioProjectViewerDataGridDataToAudioProjectEditor(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);
        }

        public static void AddAudioProjectEditorDataGridDataToAudioProjectViewer(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> audioProjectEditorRow)
        {
            AudioProjectHelpers.InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid, audioProjectEditorRow);
        }

        private static void AddActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, System.Collections.Generic.Dictionary<string, string> audioProjectEditorRow)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioProjectEditorRow = audioProjectEditorRow,
                    SoundBank = soundBank
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);

                s_logger.Here().Information($"Added Action Event data to SoundBank: {soundBank.Name}");
            }
        }

        private static void AddDialogueEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, System.Collections.Generic.Dictionary<string, string> audioProjectEditorRow)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            var parameters = new AudioProjectDataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectEditorRow = audioProjectEditorRow,
                AudioRepository = audioRepository,
                DialogueEvent = dialogueEvent
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);

            s_logger.Here().Information($"Added Dialogue Event data to Dialogue Event: {dialogueEvent.Name}");
        }

        private static void AddStateGroupData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, System.Collections.Generic.Dictionary<string, string> audioProjectEditorRow)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            var parameters = new AudioProjectDataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                StateGroup = stateGroup,
                AudioProjectEditorRow = audioProjectEditorRow
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
            audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);

            s_logger.Here().Information($"Added State Group data to State Group: {stateGroup.Name}");
        }

        public static void HandleEditingAudioProjectViewerData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer = false;

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                EditActionEventSoundBankData(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                EditDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                EditStateGroupData(audioEditorViewModel, audioProjectService);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private static void EditActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank)
            {
                DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);
                AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioProjectService = audioProjectService,
                    AudioEditorViewModel = audioEditorViewModel,
                    SoundBank = soundBank
                };

                audioEditorViewModel.AudioProjectViewerViewModel.ShowSettingsFromAudioProjectViewerItem();

                if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
                    audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

                s_logger.Here().Information($"Edited Action Event data from SoundBank: {soundBank.Name}");
            }
        }

        private static void EditDialogueEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            var parameters = new AudioProjectDataServiceParameters
            {
                AudioProjectService = audioProjectService,
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = audioRepository,
                DialogueEvent = dialogueEvent
            };

            audioEditorViewModel.AudioProjectViewerViewModel.ShowSettingsFromAudioProjectViewerItem();

            if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
                audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            s_logger.Here().Information($"Edited Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
        }

        private static void EditStateGroupData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            var parameters = new AudioProjectDataServiceParameters
            {
                AudioProjectService = audioProjectService,
                AudioEditorViewModel = audioEditorViewModel,
                StateGroup = stateGroup
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            s_logger.Here().Information($"Edited State Group data from State Group: {stateGroup.Name}");
        }

        public static void HandleRemovingAudioProjectViewerData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                RemoveActionEventData(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                RemoveDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                RemoveStateGroupData(audioEditorViewModel, audioProjectService);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private static void RemoveActionEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioProjectService = audioProjectService,
                    AudioEditorViewModel = audioEditorViewModel,
                    SoundBank = soundBank
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

                s_logger.Here().Information($"Removed Action Event data from SoundBank: {soundBank.Name}");
            }
        }

        private static void RemoveDialogueEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            var parameters = new AudioProjectDataServiceParameters
            {
                AudioProjectService = audioProjectService,
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = audioRepository,
                DialogueEvent = dialogueEvent
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            s_logger.Here().Information($"Removed Dialogue Event data from Dialogue Event: {dialogueEvent.Name}");
        }

        private static void RemoveStateGroupData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            var parameters = new AudioProjectDataServiceParameters
            {
                AudioProjectService = audioProjectService,
                AudioEditorViewModel = audioEditorViewModel,
                StateGroup = stateGroup
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            s_logger.Here().Information($"Removed State Group data from State Group: {stateGroup.Name}");
        }
    }
}
