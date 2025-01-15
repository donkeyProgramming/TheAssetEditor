using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectHelpers
    {
        public static void AddAudioProjectViewerDataGridDataToAudioProjectEditor(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);
        }

        public static void AddAudioProjectEditorDataGridDataToAudioProjectViewer(AudioEditorViewModel audioEditorViewModel, Dictionary<string, object> audioProjectEditorRow)
        {
            InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioProjectEditorRow);
        }

        public static Dictionary<string, object> ExtractRowFromSingleRowDataGrid(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository)
        {
            var newRow = new Dictionary<string, object>();

            foreach (var kvp in audioEditorViewModel.AudioProjectEditorSingleRowDataGrid[0])
            {
                var columnName = kvp.Key;
                var cellValue = kvp.Value;
                if (columnName == "AudioFiles" && cellValue is List<string> stringList)
                {
                    var newList = new List<string>(stringList);
                    newRow[columnName] = newList;
                }
                else
                {
                    if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                    {
                        var stateGroup = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, RemoveExtraUnderscoresFromString(columnName));
                        var stateGroupsWithAnyState = audioRepository.StateGroupsWithStates
                            .Where(kvp => kvp.Value.Contains("Any"))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        if (cellValue.ToString() == string.Empty && columnName != "AudioFilesDisplay" && stateGroupsWithAnyState.ContainsKey(stateGroup))
                        {
                            newRow[columnName] = "Any";
                            continue;
                        }
                    }
                    newRow[columnName] = cellValue.ToString();
                }
            }

            return newRow;
        }

        public static ActionEvent GetMatchingActionEvent(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, Dictionary<string, object> dataGridRow, SoundBank actionEventSoundBank)
        {
            if (dataGridRow.TryGetValue("Event", out var eventName))
            {
                foreach (var actionEvent in actionEventSoundBank.ActionEvents)
                    if (actionEvent.Name == eventName.ToString())
                        return actionEvent;
            }

            return null;
        }

        public static StatePath GetStatePathFromDialogueEvent(IAudioRepository audioRepository, Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();

            var stateGroupsWithQualifiers = audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[selectedDialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers.Keys)
            {
                if (dataGridRow.TryGetValue(AddExtraUnderscoresToString(stateGroupWithQualifier), out var cellValue))
                {
                    var state = cellValue;

                    var statePathNode = new StatePathNode
                    {
                        StateGroup = new StateGroup(),
                        State = new State()
                    };
                    statePathNode.StateGroup.Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, stateGroupWithQualifier);
                    statePathNode.State.Name = state.ToString();
                    statePath.Nodes.Add(statePathNode);
                }
            }

            return statePath;
        }

        public static StatePath GetMatchingDecisionNode(StatePath comparisonStatePath, DialogueEvent selectedDialogueEvent)
        {
            foreach (var statePath in selectedDialogueEvent.DecisionTree)
            {
                var stateGroups = statePath.Nodes.Select(node => node.StateGroup.Name).ToList();
                var states = statePath.Nodes.Select(node => node.State.Name).ToList();
                var comparisonStateGroups = comparisonStatePath.Nodes.Select(node => node.StateGroup.Name).ToList();
                var comparisonStates = comparisonStatePath.Nodes.Select(node => node.State.Name).ToList();
                if (states.SequenceEqual(comparisonStates) && stateGroups.SequenceEqual(comparisonStateGroups))
                    return statePath;
            }

            return null;
        }

        public static State GetMatchingState(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, Dictionary<string, object> dataGridRow, StateGroup moddedStateGroup)
        {
            foreach (var kvp in dataGridRow)
            {
                var columnName = RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;

                foreach (var state in moddedStateGroup.States)
                    if (moddedStateGroup.Name == columnName && state.Name == columnValue)
                        return state;
            }

            return null;
        }

        public static void InsertDataGridRowAlphabetically(ObservableCollection<Dictionary<string, object>> audioProjectEditorFullDataGrid, Dictionary<string, object> newRow)
        {
            var insertIndex = 0;
            var newValue = newRow.First().Value.ToString();

            for (var i = 0; i < audioProjectEditorFullDataGrid.Count; i++)
            {
                var currentValue = audioProjectEditorFullDataGrid[i].First().Value.ToString();
                var comparison = string.Compare(newValue, currentValue, StringComparison.Ordinal);
                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }
                else if (comparison == 0)
                    insertIndex = i + 1;
                else
                    insertIndex = i + 1;
            }

            audioProjectEditorFullDataGrid.Insert(insertIndex, newRow);
        }

        public static void InsertStatePathAlphabetically(DialogueEvent selectedDialogueEvent, StatePath statePath)
        {
            var newStateName = statePath.Nodes.First().State.Name;
            var decisionTree = selectedDialogueEvent.DecisionTree;
            var insertIndex = 0;

            for (var i = 0; i < decisionTree.Count; i++)
            {
                var existingStateName = decisionTree[i].Nodes.First().State.Name;
                var comparison = string.Compare(newStateName, existingStateName, StringComparison.Ordinal);
                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }
                else if (comparison == 0)
                    insertIndex = i + 1;
                else
                    insertIndex = i + 1;
            }

            decisionTree.Insert(insertIndex, statePath);
        }

        public static void InsertActionEventAlphabetically(SoundBank selectedSoundBank, ActionEvent newEvent)
        {
            var events = selectedSoundBank.ActionEvents;
            var newEventName = newEvent.Name;
            var insertIndex = 0;

            for (var i = 0; i < events.Count; i++)
            {
                var existingEventName = events[i].Name;
                var comparison = string.Compare(newEventName, existingEventName, StringComparison.Ordinal);
                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }
                else if (comparison == 0)
                    insertIndex = i + 1;
                else
                    insertIndex = i + 1;
            }

            events.Insert(insertIndex, newEvent);
        }

        public static void InsertStateAlphabetically(StateGroup moddedStateGroup, State newState)
        {
            var states = moddedStateGroup.States;
            var newStateName = newState.Name;
            var insertIndex = 0;

            for (var i = 0; i < states.Count; i++)
            {
                var existingStateName = states[i].Name;
                var comparison = string.Compare(newStateName, existingStateName, StringComparison.Ordinal);

                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }
                else if (comparison == 0)
                    insertIndex = i + 1;
                else
                    insertIndex = i + 1;
            }

            states.Insert(insertIndex, newState);
        }

        public static string AddExtraUnderscoresToString(string wtfWPF)
        {
            return wtfWPF.Replace("_", "__");
        }

        public static string RemoveExtraUnderscoresFromString(string wtfWPF)
        {
            return wtfWPF.Replace("__", "_");
        }
    }
}
