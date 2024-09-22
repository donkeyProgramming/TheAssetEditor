using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorHelpers
    {
        public static SoundBank GetSoundBankFromSelectedAudioType(string selectedAudioType, ObservableCollection<SoundBank> soundBanks)
        {
            SoundBank soundBank = null;

            foreach (var existingSoundBank in soundBanks)
            {
                if (existingSoundBank.Name == selectedAudioType)
                {
                    soundBank = existingSoundBank;
                    break;
                }
            }

            return soundBank;
        }

        public static DecisionNode GetMatchingDecisionNode(StatePath comparisonStatePath, DialogueEvent selectedDialogueEvent)
        {
            foreach (var decisionNode in selectedDialogueEvent.DecisionTree)
            {
                var stateGroups = decisionNode.StatePath.Nodes.Select(node => node.StateGroup.Name).ToList();
                var comparisonStateGroups = comparisonStatePath.Nodes.Select(node => node.StateGroup.Name).ToList();

                if (!stateGroups.SequenceEqual(comparisonStateGroups))
                    return null;

                var states = decisionNode.StatePath.Nodes.Select(node => node.State).ToList();
                var comparisonStates = comparisonStatePath.Nodes.Select(node => node.State).ToList();

                if (states.SequenceEqual(comparisonStates))
                    return decisionNode;
            }

            return null;
        }

        public static void GetModdedStates(ObservableCollection<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedtates)
        {
            if (stateGroupsWithModdedtates == null)
                stateGroupsWithModdedtates = new Dictionary<string, List<string>>();

            else
                stateGroupsWithModdedtates.Clear();

            foreach (var stateGroup in moddedStateGroups)
            {
                if (stateGroup.States != null && stateGroup.States.Count > 0)
                {
                    foreach (var state in stateGroup.States)
                    {

                        if (!stateGroupsWithModdedtates.ContainsKey(stateGroup.Name))
                            stateGroupsWithModdedtates[stateGroup.Name] = new List<string>();

                        stateGroupsWithModdedtates[stateGroup.Name].Add(state.Name);
                    }
                }
            }
        }

        public static void SortSoundBanksByName(ObservableCollection<SoundBank> audioProjectItems)
        {
            var sortedSoundBanks = audioProjectItems.OrderBy(soundBank => soundBank.Name).ToList();

            audioProjectItems.Clear();

            foreach (var soundBank in sortedSoundBanks)
                audioProjectItems.Add(soundBank);
        }


        public static void InsertStatePathAlphabetically(DialogueEvent selectedDialogueEvent, DecisionNode decisionNode, StatePath newStatePath)
        {
            var newStateName = newStatePath.Nodes.First().State.Name;
            var decisionTree = selectedDialogueEvent.DecisionTree;
            var insertIndex = 0;

            for (var i = 0; i < decisionTree.Count; i++)
            {
                var existingStateName = decisionTree[i].StatePath.Nodes.First().State.Name;

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

            decisionTree.Insert(insertIndex, decisionNode);
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

        public static void AddDataGridRowToModdedStates(Dictionary<string, object> dataGridRow, StateGroup stateGroup)
        {
            var state = new State();

            foreach (var kvp in dataGridRow)
                state.Name = kvp.Value.ToString();

            InsertStateAlphabetically(stateGroup, state);
        }

        public static void AddDataGridRowToActionEventSoundBank(Dictionary<string, object> dataGridRow, SoundBank selectedSoundBank)
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

        public static void AddDataGridRowToDialogueEvent(Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent)
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

                    statePathNode.StateGroup.Name = GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, stateGroupWithQualifier);
                    statePathNode.State.Name = kvp.Value.ToString();

                    statePath.Nodes.Add(statePathNode);
                }
            }

            decisionNode.StatePath = statePath;

            InsertStatePathAlphabetically(selectedDialogueEvent, decisionNode, statePath);
        }

        public static void RemoveDataGridRowFromDialogueEvent(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, Dictionary<string, object> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();

            foreach (var kvp in dataGridRow)
            {
                if (kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay")
                {
                    var stateGroupWithQualifierAndExtraUnderscores = kvp.Key;
                    var stateGroupWithQualifier = RemoveExtraUnderscoresFromString(stateGroupWithQualifierAndExtraUnderscores);
                    var state = kvp.Value;

                    var statePathNode = new StatePathNode { };
                    statePathNode.StateGroup.Name = GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, stateGroupWithQualifier);
                    statePathNode.State.Name = state.ToString();

                    statePath.Nodes.Add(statePathNode);
                }
            }

            var matchingStatePath = GetMatchingDecisionNode(statePath, selectedDialogueEvent);

            if (matchingStatePath != null)
            {
                selectedDialogueEvent.DecisionTree.Remove(matchingStatePath);
                audioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }

        public static string GetStateGroupFromStateGroupWithQualifier(string dialogueEvent, string stateGroupWithQualifier)
        {
            if (DialogueEventsWithStateGroupsWithQualifiersAndStateGroups.TryGetValue(dialogueEvent, out var stateGroupDictionary))
            {
                if (stateGroupDictionary.TryGetValue(stateGroupWithQualifier, out var stateGroup))
                    return stateGroup;
            }

            return null;
        }

        public static void ClearDataGrid(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid)
        {
            audioProjectViewerDataGrid.Clear();
        }

        public static void ClearDataGridColumns(string dataGridName)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.Columns.Clear();
        }

        public static T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && child is FrameworkElement element && element.Name == name)
                    return typedChild;

                else
                {
                    var foundChild = FindVisualChild<T>(child, name);

                    if (foundChild != null)
                        return foundChild;
                }
            }

            return null;
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while ((child = VisualTreeHelper.GetParent(child)) != null)
            {
                if (child is T parent)
                    return parent;
            }
            return null;
        }

        public static DataGrid GetDataGrid(string dataGridName)
        {
            var mainWindow = Application.Current.MainWindow;
            return FindVisualChild<DataGrid>(mainWindow, dataGridName);
        }

        // Apparently WPF doesn't_like_underscores so double them up in order for them to be displayed in the UI.
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
