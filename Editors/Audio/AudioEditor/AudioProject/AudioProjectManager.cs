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

                    AddRowDataToActionEventSoundBank(singleRowDataGridRow, selectedSoundBank);
                }
                else if (selectedSoundBank.Type == GameSoundBankType.MusicEventSoundBank.ToString())
                {
                    throw new NotImplementedException();
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                SetAudioProjectEditorSingleRowDataGridToDialogueEvent(audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, selectedDialogueEvent);

                AddRowDataToDialogueEvent(singleRowDataGridRow, selectedDialogueEvent, audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
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
                else if (selectedSoundBank.Type == GameSoundBankType.MusicEventSoundBank.ToString())
                {
                    throw new NotImplementedException();
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
                else if (selectedSoundBank.Type == GameSoundBankType.MusicEventSoundBank.ToString())
                {
                    throw new NotImplementedException();
                }
            }
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                RemoveRowDataFromDialogueEvent(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedDialogueEvent, audioProjectService, audioRepository);
            else if (audioEditorViewModel._selectedAudioProjectTreeItem is StateGroup selectedModdedStateGroup)
                RemoveRowDataFromModdedStates(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioEditorViewModel.SelectedDataGridRows, selectedModdedStateGroup);

            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
        }

        public static void AddRowDataToActionEventSoundBank(Dictionary<string, object> dataGridRow, SoundBank selectedSoundBank)
        {
            var soundBankEvent = new ActionEvent();

            foreach (var kvp in dataGridRow)
            {
                if (kvp.Key == "AudioFiles")
                {
                    var filePaths = kvp.Value as List<string>;
                    var fileNames = filePaths.Select(Path.GetFileName);
                    var fileNamesString = string.Join(", ", fileNames);

                    soundBankEvent.AudioFiles = filePaths;
                    soundBankEvent.AudioFilesDisplay = fileNamesString;
                }
                else if (kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay")
                    soundBankEvent.Name = kvp.Value.ToString();
            }

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

        public static void AddRowDataToDialogueEvent(Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository)
        {
            var decisionNode = new DecisionNode();
            var statePath = new StatePath();

            foreach (var kvp in dataGridRow)
            {
                if (kvp.Key == "AudioFiles")
                {
                    var filePaths = kvp.Value as List<string>;
                    var fileNames = filePaths.Select(Path.GetFileName);
                    var fileNamesString = string.Join(", ", fileNames);

                    decisionNode.AudioFiles = filePaths;
                    decisionNode.AudioFilesDisplay = fileNamesString;
                }
                else if (kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay")
                {
                    var stateGroupWithQualifierAndExtraUnderscores = kvp.Key;
                    var stateGroupWithQualifier = RemoveExtraUnderscoresFromString(stateGroupWithQualifierAndExtraUnderscores);

                    var statePathNode = new StatePathNode
                    {
                        StateGroup = new StateGroup(),
                        State = new State()
                    };
                    statePathNode.StateGroup.Name = GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, stateGroupWithQualifier, dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository);
                    statePathNode.State.Name = kvp.Value.ToString();
                    statePath.Nodes.Add(statePathNode);
                }
            }

            decisionNode.StatePath = statePath;

            InsertStatePathAlphabetically(selectedDialogueEvent, decisionNode, statePath);
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
            var state = new State();

            foreach (var kvp in dataGridRow)
                state.Name = kvp.Value.ToString();

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
