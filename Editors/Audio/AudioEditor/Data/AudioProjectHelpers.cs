using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data
{
    public class AudioProjectHelpers
    {
        public static void AddAudioProjectViewerDataGridDataToAudioProjectEditor(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0]);
        }

        public static void AddAudioProjectEditorDataGridDataToAudioProjectViewer(AudioEditorViewModel audioEditorViewModel, Dictionary<string, object> audioProjectEditorRow)
        {
            InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid, audioProjectEditorRow);
        }

        public static Dictionary<string, object> ExtractRowFromSingleRowDataGrid(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            var newRow = new Dictionary<string, object>();

            foreach (var kvp in audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0])
            {
                var columnName = kvp.Key;
                var cellValue = kvp.Value;

                if (columnName == "AudioFiles" && cellValue is List<string> stringList)
                {
                    var newList = new List<string>(stringList);
                    newRow[columnName] = newList;
                }
                else
                    newRow[columnName] = cellValue.ToString();
            }

            return newRow;
        }

        public static SoundBank GetSoundBankFromName(IAudioProjectService audioProjectService, string soundBankName)
        {
            return audioProjectService.AudioProject.SoundBanks
                .FirstOrDefault(soundBank => soundBank.Name == soundBankName);
        }

        public static DialogueEvent GetDialogueEventFromName(IAudioProjectService audioProjectService, string dialogueEventName)
        {
            return audioProjectService.AudioProject.SoundBanks
                .SelectMany(soundBank => soundBank.DialogueEvents)
                .FirstOrDefault(dialogueEvent => dialogueEvent.Name == dialogueEventName);
        }

        public static StateGroup GetStateGroupFromName(IAudioProjectService audioProjectService, string stateGroupName)
        {
            return audioProjectService.AudioProject.StateGroups
                .FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);
        }

        public static AudioProjectTreeNode GetAudioProjectTreeNodeFromName(ObservableCollection<AudioProjectTreeNode> audioProjecTree, string nodeName)
        {
            foreach (var node in audioProjecTree)
            {
                if (node.Name == nodeName)
                    return node;

                var childNode = GetAudioProjectTreeNodeFromName(node.Children, nodeName);
                if (childNode != null)
                    return childNode;
            }

            return null;
        }

        public static ActionEvent GetActionEventMatchingWithDataGridRow(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, Dictionary<string, object> dataGridRow, SoundBank actionEventSoundBank)
        {
            if (dataGridRow.TryGetValue("Event", out var eventName))
            {
                foreach (var actionEvent in actionEventSoundBank.ActionEvents)
                    if (actionEvent.Name == eventName.ToString())
                        return actionEvent;
            }

            return null;
        }

        public static StatePath GetStatePathMatchingWithDataGridRow(IAudioRepository audioRepository, Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent)
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

        public static State GetStateMatchingWithDataGridRow(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, Dictionary<string, object> dataGridRow, StateGroup moddedStateGroup)
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

        public static void InsertDataGridRowAlphabetically(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, Dictionary<string, object> newRow)
        {
            var insertIndex = 0;
            var newValue = newRow.First().Value.ToString();

            for (var i = 0; i < audioProjectViewerDataGrid.Count; i++)
            {
                var currentValue = audioProjectViewerDataGrid[i].First().Value.ToString();
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

            audioProjectViewerDataGrid.Insert(insertIndex, newRow);
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
