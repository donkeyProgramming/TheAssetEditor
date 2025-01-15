using Editors.Audio.AudioEditor.AudioSettingsEditor;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.CopyPasteHandler;
using static Editors.Audio.AudioEditor.AudioProjectExplorer.DialogueEventFilter;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectItemLoader
    {
        private static readonly ILogger s_logger = Logging.Create<AudioProjectItemLoader>();

        public static void HandleSelectedTreeViewItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor";
            audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer";

            // Set filtering properties
            audioEditorViewModel.AudioProjectExplorerViewModel.IsDialogueEventPresetFilterEnabled = false;
            audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset = null;

            // Set button enablement
            audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsUpdateRowButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsRemoveRowButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsAddAudioFilesButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsPlayAudioButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;

            // Set AudioSettings visibility
            audioEditorViewModel.AudioSettingsViewModel.IsAudioSettingsVisible = false;
            audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = false;

            // Reset DataGrids
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid);
            DataGridHelpers.ClearDataGridCollection(audioEditorViewModel.AudioProjectEditorFullDataGrid);
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag));
            DataGridHelpers.ClearDataGridColumns(DataGridHelpers.GetDataGridByTag(audioEditorViewModel.AudioProjectEditorFullDataGridTag));

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {selectedSoundBank.Name}";
                    audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {selectedSoundBank.Name}";

                    AudioSettingsEditorViewModel.SetAudioSettingsEnablement(audioEditorViewModel.AudioSettingsViewModel);

                    var parameters = new AudioProjectDataServiceParameters
                    {
                        AudioEditorViewModel = audioEditorViewModel,
                        AudioProjectService = audioProjectService,
                        SoundBank = selectedSoundBank
                    };

                    var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedSoundBank);
                    audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                    audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                    audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
                    audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

                    s_logger.Here().Information($"Loaded Action Event SoundBank: {selectedSoundBank.Name}");
                }
                else if (selectedSoundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                {
                    // Workaround for using ref with the MVVM toolkit as you can't pass a property by ref, so instead pass a field that is set to the property by ref then assign the ref field to the property
                    var isDialogueEventPresetFilterEnabled = audioEditorViewModel.AudioProjectExplorerViewModel.IsDialogueEventPresetFilterEnabled;
                    var dialogueEventPresets = audioEditorViewModel.AudioProjectExplorerViewModel.DialogueEventPresets;
                    HandleDialogueEventsPresetFilter(selectedSoundBank.Name, ref dialogueEventPresets, audioEditorViewModel.AudioProjectExplorerViewModel.DialogueEventSoundBankFiltering, audioEditorViewModel.AudioProjectExplorerViewModel.SelectedDialogueEventPreset, ref isDialogueEventPresetFilterEnabled);
                    audioEditorViewModel.AudioProjectExplorerViewModel.IsDialogueEventPresetFilterEnabled = isDialogueEventPresetFilterEnabled;
                    audioEditorViewModel.AudioProjectExplorerViewModel.DialogueEventPresets = dialogueEventPresets;
                }
            }
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AudioProjectHelpers.AddExtraUnderscoresToString(selectedDialogueEvent.Name)}";
                audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AudioProjectHelpers.AddExtraUnderscoresToString(selectedDialogueEvent.Name)}";

                AudioSettingsEditorViewModel.SetAudioSettingsEnablement(audioEditorViewModel.AudioSettingsViewModel);

                // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
                audioProjectService.BuildStateGroupsWithModdedStatesRepository(audioProjectService.AudioProject.States, audioProjectService.StateGroupsWithModdedStatesRepository);

                if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                    audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioProjectService = audioProjectService,
                    AudioRepository = audioRepository,
                    DialogueEvent = selectedDialogueEvent
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedDialogueEvent);
                audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

                s_logger.Here().Information($"Loaded DialogueEvent: {selectedDialogueEvent.Name}");
            }
            else if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AudioProjectHelpers.AddExtraUnderscoresToString(selectedModdedStateGroup.Name)}";
                audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AudioProjectHelpers.AddExtraUnderscoresToString(selectedModdedStateGroup.Name)}";

                var parameters = new AudioProjectDataServiceParameters
                {
                    AudioEditorViewModel = audioEditorViewModel,
                    AudioRepository = audioRepository,
                    AudioProjectService = audioProjectService,
                    StateGroup = selectedModdedStateGroup
                };

                var audioProjectDataServiceInstance = AudioProjectDataServiceFactory.GetService(selectedModdedStateGroup);
                audioProjectDataServiceInstance.SetAudioProjectEditorDataGridData(parameters);
                audioProjectDataServiceInstance.ConfigureAudioProjectEditorDataGrid(parameters);
                audioProjectDataServiceInstance.ConfigureAudioProjectViewerDataGrid(parameters);
                audioProjectDataServiceInstance.SetAudioProjectViewerDataGridData(parameters);

                s_logger.Here().Information($"Loaded StateGroup: {selectedModdedStateGroup.Name}");
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
            SetIsCopyEnabled(audioEditorViewModel);
            SetIsPasteEnabled(audioEditorViewModel, audioRepository, audioProjectService);
        }
    }
}
