using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.TreeViewItemLoader;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProject
{
    public class AudioProjectManager
    {
        public static void HandleAddingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var singleRowDataGridRow = ExtractRowFromSingleRowDataGrid(audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, audioRepository.StateGroupsWithStates, audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, audioEditorViewModel._selectedAudioProjectTreeItem);
            InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectEditorFullDataGrid, singleRowDataGridRow);

            ClearDataGrid(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    SetAudioProjectEditorSingleRowDataGridToActionEventSoundBank(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                    AddRowDataToActionEventSoundBank(audioEditorViewModel.AudioSettingsViewModel, singleRowDataGridRow, selectedSoundBank);
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                SetAudioProjectEditorSingleRowDataGridToDialogueEvent(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, selectedDialogueEvent);

                AddRowDataToDialogueEvent(audioEditorViewModel.AudioSettingsViewModel, singleRowDataGridRow, selectedDialogueEvent, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                SetAudioProjectEditorSingleRowDataGridToModdedStateGroup(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, selectedModdedStateGroup.Name);

                AddRowDataToModdedStates(singleRowDataGridRow, selectedModdedStateGroup);
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void HandleUpdatingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                {
                    ClearDataGrid(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                    audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);

                    RemoveRowDataFromActionEventSoundBank(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedSoundBank);
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                ClearDataGrid(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);

                RemoveRowDataFromDialogueEvent(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedDialogueEvent, audioProjectService, audioRepository);
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
            {
                ClearDataGrid(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid);

                audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);

                RemoveRowDataFromModdedStates(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedModdedStateGroup);
            }

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void HandleRemovingRowData(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            if (audioEditorViewModel._selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                    RemoveRowDataFromActionEventSoundBank(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedSoundBank);
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                RemoveRowDataFromDialogueEvent(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedDialogueEvent, audioProjectService, audioRepository);
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
                RemoveRowDataFromModdedStates(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedModdedStateGroup);

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void AddRowDataToActionEventSoundBank(AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, object> dataGridRow, SoundBank selectedSoundBank)
        {
            var soundBankEvent = new ActionEvent();

            if (dataGridRow.TryGetValue("AudioFiles", out var audioFiles))
            {
                var filePaths = audioFiles as List<string>;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                soundBankEvent.AudioFiles = filePaths;
                soundBankEvent.AudioFilesDisplay = fileNamesString;
            }

            if (dataGridRow.TryGetValue("AudioSettings", out var audioSettings))
                soundBankEvent.AudioSettings = AudioSettingsViewModel.BuildAudioSettings(audioSettingsViewModel);

            InsertActionEventAlphabetically(selectedSoundBank, soundBankEvent);
        }

        public static void RemoveRowDataFromActionEventSoundBank(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, ObservableCollection<Dictionary<string, object>> dataGridRows, SoundBank selectedActionEventSoundBank)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = dataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = GetMatchingActionEvent(audioProjectEditorFullDataGrid, dataGridRow, selectedActionEventSoundBank);
                selectedActionEventSoundBank.ActionEvents.Remove(actionEvent);
                audioProjectEditorFullDataGrid.Remove(dataGridRow);
            }
        }

        public static void AddRowDataToDialogueEvent(AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository)
        {
            var statePath = new StatePath();

            var stateGroupsWithQualifiers = dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[selectedDialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers.Keys)
            {
                if (dataGridRow.TryGetValue(AddExtraUnderscoresToString(stateGroupWithQualifier), out var cellValue))
                {
                    var statePathNode = new StatePathNode
                    {
                        StateGroup = new StateGroup(),
                        State = new State()
                    };
                    statePathNode.StateGroup.Name = stateGroupsWithQualifiers[stateGroupWithQualifier];
                    statePathNode.State.Name = cellValue.ToString();
                    statePath.Nodes.Add(statePathNode);
                }
            }

            if (dataGridRow.TryGetValue("AudioFiles", out var audioFiles))
            {
                var filePaths = audioFiles as List<string>;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                statePath.AudioFiles = filePaths;
                statePath.AudioFilesDisplay = fileNamesString;
            }

            if (dataGridRow.TryGetValue("AudioSettings", out var audioSettings))
                statePath.AudioSettings = AudioSettingsViewModel.BuildAudioSettings(audioSettingsViewModel);

            InsertStatePathAlphabetically(selectedDialogueEvent, statePath);
        }

        public static void RemoveRowDataFromDialogueEvent(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, ObservableCollection<Dictionary<string, object>> dataGridRows, DialogueEvent selectedDialogueEvent, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = dataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = GetStatePathFromDialogueEvent(dataGridRow, selectedDialogueEvent, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
                var matchingStatePath = GetMatchingDecisionNode(statePath, selectedDialogueEvent);
                if (matchingStatePath != null)
                {
                    selectedDialogueEvent.DecisionTree.Remove(matchingStatePath);
                    audioProjectEditorFullDataGrid.Remove(dataGridRow);
                }
                else if (audioProjectService.DialogueEventsWithStateGroupsWithIntegrityError.ContainsKey(selectedDialogueEvent.Name))
                {
                    // This is a backup in case there's an integrity issue with the State Groups in a Dialogue Event either due to a change to the Dialogue Event's State Groups by CA or the user messing with the file.
                    // As it happens this is probably a better way of removing items given they should all be sorted correctly and therefore have the same index however it's not as safe as finding the exact match by proof so a backup it remains.
                    var dataGridRowIndex = dataGridRowsCopy.IndexOf(dataGridRow);
                    var decisionNode = selectedDialogueEvent.DecisionTree[dataGridRowIndex];
                    selectedDialogueEvent.DecisionTree.Remove(decisionNode);
                    audioProjectEditorFullDataGrid.Remove(dataGridRow);
                }
            }
        }

        public static void AddRowDataToModdedStates(Dictionary<string, object> dataGridRow, StateGroup stateGroup)
        {
            var rowData = dataGridRow.First();
            var state = new State();
            state.Name = rowData.Value.ToString();
            InsertStateAlphabetically(stateGroup, state);
        }

        public static void RemoveRowDataFromModdedStates(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, ObservableCollection<Dictionary<string, object>> dataGridRows, StateGroup selectedStateGroup)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = dataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = GetMatchingState(audioProjectEditorFullDataGrid, dataGridRow, selectedStateGroup);
                selectedStateGroup.States.Remove(state);
                audioProjectEditorFullDataGrid.Remove(dataGridRow);
            }
        }
    }
}
