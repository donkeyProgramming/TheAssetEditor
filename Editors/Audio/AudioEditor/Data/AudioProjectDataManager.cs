using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{
    public class AudioProjectDataManager
    {
        public static void HandleAddingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var audioProjectEditorRow = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository, audioProjectService);
            AudioProjectHelpers.AddAudioProjectEditorDataGridDataToAudioProjectViewer(audioEditorViewModel, audioProjectEditorRow);

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                AddActionEventSoundBankData(audioEditorViewModel, audioProjectService, audioProjectEditorRow);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                AddDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository, audioProjectEditorRow);
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                AddStateGroupData(audioEditorViewModel, audioProjectService, audioProjectEditorRow);

            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
        }

        private static void AddActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, System.Collections.Generic.Dictionary<string, string> audioProjectEditorRow)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
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
        }

        public static void HandleEditingAudioProjectViewerData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
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
            if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
            {
                DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);
                AudioProjectHelpers.AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioProjectService = audioProjectService,
                    AudioEditorViewModel = audioEditorViewModel,
                    SoundBank = soundBank
                };
                if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
                {
                    var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0], soundBank);
                    audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsFromAudioProjectItem(actionEvent.AudioSettings);
                    audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();
                }

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
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

            if (audioEditorViewModel.AudioSettingsViewModel.ShowSettingsFromAudioProjectViewer)
            {
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(audioRepository, audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0], dialogueEvent);
                audioEditorViewModel.AudioSettingsViewModel.SetAudioSettingsFromAudioProjectItem(statePath.AudioSettings);
                audioEditorViewModel.AudioSettingsViewModel.DisableAllAudioSettings();
            }

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
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
            if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioProjectService = audioProjectService,
                    AudioEditorViewModel = audioEditorViewModel,
                    SoundBank = soundBank
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
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
        }
    }
}
