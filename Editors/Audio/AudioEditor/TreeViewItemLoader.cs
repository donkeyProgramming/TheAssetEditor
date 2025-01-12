using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.CopyPasteHandler;
using static Editors.Audio.AudioEditor.DataGrids.FullDataGridConfiguration;
using static Editors.Audio.AudioEditor.DataGrids.SingleRowDataGridConfiguration;
using static Editors.Audio.AudioEditor.DialogueEventFilter;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor
{
    public class TreeViewItemLoader
    {
        private static readonly ILogger s_logger = Logging.Create<TreeViewItemLoader>();

        public static void HandleSelectedTreeViewItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor";
            audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer";

            // Set filtering properties
            audioEditorViewModel.IsDialogueEventPresetFilterEnabled = false;
            audioEditorViewModel.SelectedDialogueEventPreset = null;

            // Set button enablement
            audioEditorViewModel.IsAddRowButtonEnabled = false;
            audioEditorViewModel.IsUpdateRowButtonEnabled = false;
            audioEditorViewModel.IsRemoveRowButtonEnabled = false;
            audioEditorViewModel.IsAddAudioFilesButtonEnabled = false;
            audioEditorViewModel.IsPlayAudioButtonEnabled = false;
            audioEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;

            // Set AudioSettings visibility
            audioEditorViewModel.AudioSettingsViewModel.IsAudioSettingsVisible = false;
            audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = false;

            // Reset DataGrids
            ClearDataGrid(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);
            ClearDataGrid(audioEditorViewModel.AudioProjectEditorFullDataGrid);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorFullDataGridTag);

            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    audioEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {selectedSoundBank.Name}";
                    audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {selectedSoundBank.Name}";

                    AudioSettingsViewModel.SetAudioSettingsEnablement(audioEditorViewModel.AudioSettingsViewModel);

                    ConfigureAudioProjectEditorSingleRowDataGridForActionEventSoundBank(audioEditorViewModel, audioProjectService, audioRepository);
                    SetAudioProjectEditorSingleRowDataGridToActionEventSoundBank(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                    ConfigureAudioProjectEditorFullDataGridForActionEventSoundBank(audioEditorViewModel, audioRepository, selectedSoundBank);
                    SetAudioProjectEditorFullDataGridToActionEventSoundBank(audioEditorViewModel.AudioProjectEditorFullDataGrid, selectedSoundBank);

                    s_logger.Here().Information($"Loaded Action Event SoundBank: {selectedSoundBank.Name}");
                }
                else if (selectedSoundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                {
                    // Workaround for using ref with the MVVM toolkit as you can't pass a property by ref, so instead pass a field that is set to the property by ref then assign the ref field to the property
                    var isDialogueEventPresetFilterEnabled = audioEditorViewModel.IsDialogueEventPresetFilterEnabled;
                    var dialogueEventPresets = audioEditorViewModel.DialogueEventPresets;
                    HandleDialogueEventsPresetFilter(selectedSoundBank.Name, ref dialogueEventPresets, audioEditorViewModel.DialogueEventSoundBankFiltering, audioEditorViewModel.SelectedDialogueEventPreset, ref isDialogueEventPresetFilterEnabled);
                    audioEditorViewModel.IsDialogueEventPresetFilterEnabled = isDialogueEventPresetFilterEnabled;
                    audioEditorViewModel.DialogueEventPresets = dialogueEventPresets;
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                audioEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AddExtraUnderscoresToString(selectedDialogueEvent.Name)}";
                audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AddExtraUnderscoresToString(selectedDialogueEvent.Name)}";

                AudioSettingsViewModel.SetAudioSettingsEnablement(audioEditorViewModel.AudioSettingsViewModel);

                // Rebuild StateGroupsWithModdedStates in case any have been added since the Audio Project was initialised
                audioProjectService.BuildStateGroupsWithModdedStatesRepository(audioProjectService.AudioProject.ModdedStates, audioProjectService.StateGroupsWithModdedStatesRepository);

                if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                    audioEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

                ConfigureAudioProjectEditorSingleRowDataGridForDialogueEvent(audioEditorViewModel, audioRepository, selectedDialogueEvent, audioProjectService);
                SetAudioProjectEditorSingleRowDataGridToDialogueEvent(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, selectedDialogueEvent);

                ConfigureAudioProjectEditorFullDataGridForDialogueEvent(audioEditorViewModel, audioRepository, audioProjectService, selectedDialogueEvent);
                SetAudioProjectEditorFullDataGridToDialogueEvent(audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, audioEditorViewModel.AudioProjectEditorFullDataGrid, selectedDialogueEvent);

                s_logger.Here().Information($"Loaded DialogueEvent: {selectedDialogueEvent.Name}");
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                audioEditorViewModel.AudioProjectEditorLabel = $"Audio Project Editor - {AddExtraUnderscoresToString(selectedModdedStateGroup.Name)}";
                audioEditorViewModel.AudioProjectViewerLabel = $"Audio Project Viewer - {AddExtraUnderscoresToString(selectedModdedStateGroup.Name)}";

                ConfigureAudioProjectEditorSingleRowDataGridForModdedStates(audioEditorViewModel, audioProjectService, audioRepository, selectedModdedStateGroup.Name);
                SetAudioProjectEditorSingleRowDataGridToModdedStateGroup(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, selectedModdedStateGroup.Name);

                ConfigureAudioProjectEditorFullDataGridForModdedStates(audioEditorViewModel, selectedModdedStateGroup.Name);
                SetAudioProjectEditorFullDataGridToModdedStateGroup(audioEditorViewModel.AudioProjectEditorFullDataGrid, selectedModdedStateGroup, selectedModdedStateGroup.Name);

                s_logger.Here().Information($"Loaded StateGroup: {selectedModdedStateGroup.Name}");
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
            SetIsPasteEnabled(audioEditorViewModel, audioProjectService, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        public static void SetAudioProjectEditorSingleRowDataGridToActionEventSoundBank(ObservableCollection<Dictionary<string, object>> audioProjectEditorSingleRowDataGrid)
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow["Event"] = string.Empty;
            dataGridRow["AudioFiles"] = new List<string> { };
            dataGridRow["AudioFilesDisplay"] = string.Empty;
            dataGridRow["AudioSettings"] = new AudioProject.AudioSettings();
            audioProjectEditorSingleRowDataGrid.Add(dataGridRow);
        }

        public static void SetAudioProjectEditorSingleRowDataGridToDialogueEvent(ObservableCollection<Dictionary<string, object>> audioProjectEditorSingleRowDataGrid, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository, DialogueEvent dialogueEvent)
        {
            var dataGridRow = new Dictionary<string, object>();
            var stateGroupsWithQualifiers = dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[dialogueEvent.Name];

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var columnHeader = AddExtraUnderscoresToString(stateGroupWithQualifier);
                dataGridRow[columnHeader] = "";
            }

            dataGridRow["AudioFiles"] = new List<string> { };
            dataGridRow["AudioFilesDisplay"] = string.Empty;
            dataGridRow["AudioSettings"] = new AudioProject.AudioSettings();
            audioProjectEditorSingleRowDataGrid.Add(dataGridRow);
        }

        public static void SetAudioProjectEditorSingleRowDataGridToModdedStateGroup(ObservableCollection<Dictionary<string, object>> audioProjectEditorSingleRowDataGrid, string moddedStateGroup)
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow[AddExtraUnderscoresToString(moddedStateGroup)] = string.Empty;
            audioProjectEditorSingleRowDataGrid.Add(dataGridRow);
        }

        public static void SetAudioProjectEditorFullDataGridToModdedStateGroup(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, StateGroup stateGroup, string moddedStateGroup)
        {
            foreach (var state in stateGroup.States)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow[AddExtraUnderscoresToString(moddedStateGroup)] = state.Name;
                audioProjectEditorFullDataGrid.Add(dataGridRow);
            }
        }

        public static void SetAudioProjectEditorFullDataGridToActionEventSoundBank(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, SoundBank audioProjectItem)
        {
            foreach (var soundBankEvent in audioProjectItem.ActionEvents)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow["Event"] = soundBankEvent.Name;
                dataGridRow["AudioFiles"] = soundBankEvent.AudioFiles;
                dataGridRow["AudioFilesDisplay"] = soundBankEvent.AudioFilesDisplay;
                audioProjectEditorFullDataGrid.Add(dataGridRow);
            }
        }

        public static void SetAudioProjectEditorFullDataGridToDialogueEvent(Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository, ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, DialogueEvent dialogueEvent)
        {
            foreach (var statePath in dialogueEvent.DecisionTree)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow["AudioFiles"] = statePath.AudioFiles;
                dataGridRow["AudioFilesDisplay"] = statePath.AudioFilesDisplay;

                var stateGroupsWithQualifiersList = dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[dialogueEvent.Name].ToList();
                foreach (var (node, kvp) in statePath.Nodes.Zip(stateGroupsWithQualifiersList, (node, kvp) => (node, kvp)))
                {
                    var stateGroupfromDialogueEvent = node.StateGroup.Name;
                    var stateFromDialogueEvent = node.State.Name;

                    var stateGroupWithQualifierKey = kvp.Key;
                    var stateGroup = kvp.Value;

                    if (stateGroupfromDialogueEvent == stateGroup)
                        dataGridRow[AddExtraUnderscoresToString(stateGroupWithQualifierKey)] = stateFromDialogueEvent;
                }

                audioProjectEditorFullDataGrid.Add(dataGridRow);
            }
        }
    }
}
