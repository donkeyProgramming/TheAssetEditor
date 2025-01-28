using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioProjectEditor.ButtonEnablement;
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

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                AddDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository, audioProjectEditorRow);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                AddStateGroupData(audioEditorViewModel, audioProjectService, audioProjectEditorRow);

            SetAddRowButtonEnablement(audioEditorViewModel, audioProjectService, audioRepository);
        }

        private static void AddActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, System.Collections.Generic.Dictionary<string, object> audioProjectEditorRow)
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

        private static void AddDialogueEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, System.Collections.Generic.Dictionary<string, object> audioProjectEditorRow)
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

        private static void AddStateGroupData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, System.Collections.Generic.Dictionary<string, object> audioProjectEditorRow)
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

        public static void HandleUpdatingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                UpdateActionEventSoundBankData(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                UpdateDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                UpdateStateGroupData(audioEditorViewModel, audioProjectService);

            SetAddRowButtonEnablement(audioEditorViewModel, audioProjectService, audioRepository);
        }

        private static void UpdateActionEventSoundBankData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
            {
                DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);
                AudioProjectHelpers.AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    SoundBank = soundBank
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(soundBank);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
            }
        }

        private static void UpdateDialogueEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            var parameters = new AudioProjectDataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = audioRepository,
                DialogueEvent = dialogueEvent
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(dialogueEvent);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
        }

        private static void UpdateStateGroupData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid);

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);

            var parameters = new AudioProjectDataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                StateGroup = stateGroup
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
        }

        public static void HandleRemovingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.ActionEventSoundBank)
                RemoveActionEventData(audioEditorViewModel, audioProjectService);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
                RemoveDialogueEventData(audioEditorViewModel, audioProjectService, audioRepository);

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.StateGroup)
                RemoveStateGroupData(audioEditorViewModel, audioProjectService);

            SetAddRowButtonEnablement(audioEditorViewModel, audioProjectService, audioRepository);
        }

        private static void RemoveActionEventData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
            if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
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
                AudioEditorViewModel = audioEditorViewModel,
                StateGroup = stateGroup
            };

            var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetDataService(stateGroup);
            audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
        }
    }
}
