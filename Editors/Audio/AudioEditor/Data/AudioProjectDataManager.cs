using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{
    public class AudioProjectDataManager
    {
        public static void HandleAddingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var audioProjectEditorRow = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository);
            AudioProjectHelpers.AddAudioProjectEditorDataGridDataToAudioProjectViewer(audioEditorViewModel, audioProjectEditorRow);

            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank soundBank)
            {
                if (soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    var parameters = new AudioProjectDataServiceParameters
                    {
                        AudioEditorViewModel = audioEditorViewModel,
                        AudioProjectEditorRow = audioProjectEditorRow,
                        SoundBank = soundBank
                    };

                    var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(soundBank);
                    audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                    audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioProjectEditorRow = audioProjectEditorRow,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    StateGroup = selectedModdedStateGroup,
                    AudioProjectEditorRow = audioProjectEditorRow
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedModdedStateGroup);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                audioProjectDataServiceInstance.AddAudioProjectEditorDataGridDataToAudioProject(parameters);
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void HandleUpdatingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank soundBank)
            {
                if (soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);
                    AudioProjectHelpers.AddAudioProjectViewerDataGridDataToAudioProjectEditor(audioEditorViewModel);

                    var parameters = new AudioProjectDataServiceParameters
                    {
                        AudioEditorViewModel = audioEditorViewModel,
                        SoundBank = soundBank
                    };

                    var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(soundBank);
                    audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    StateGroup = selectedModdedStateGroup
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedModdedStateGroup);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void HandleRemovingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank soundBank)
            {
                if (soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    var parameters = new AudioProjectDataServiceParameters
                    {
                        AudioEditorViewModel = audioEditorViewModel,
                        SoundBank = soundBank
                    };

                    var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(soundBank);
                    audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);

            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    StateGroup = selectedModdedStateGroup
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedModdedStateGroup);
                audioProjectDataServiceInstance.RemoveAudioProjectEditorDataGridDataFromAudioProject(parameters);
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }
    }
}
