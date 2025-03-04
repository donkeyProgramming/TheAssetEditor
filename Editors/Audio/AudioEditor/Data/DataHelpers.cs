using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.Storage;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{
    public class DataHelpers
    {
        public static Dictionary<string, string> ExtractRowFromSingleRowDataGrid(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            var newRow = new Dictionary<string, string>();

            foreach (var kvp in audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0])
            {
                var columnName = kvp.Key;
                var cellValue = kvp.Value;

                if (cellValue == null)
                    cellValue = string.Empty;

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
                .Where(soundBank => soundBank.SoundBankType == Wh3SoundBankType.DialogueEventSoundBank)
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

        public static ActionEvent GetActionEventFromDataGridRow(Dictionary<string, string> dataGridRow, SoundBank actionEventSoundBank)
        {
            var dataGridRowActionEventName = GetActionEventNameFromDataGridRow(dataGridRow);

            foreach (var actionEvent in actionEventSoundBank.ActionEvents)
            {                    
                if (actionEvent.Name == dataGridRowActionEventName)
                    return actionEvent;
            }

            return null;
        }

        public static ActionEvent CreateActionEvent(AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, string> dataGridRow)
        {
            var actionEvent = new ActionEvent();

            if (dataGridRow.TryGetValue("Event", out var eventName))
            {
                actionEvent.Name = eventName.ToString();

                var audioFiles = audioSettingsViewModel.AudioFiles;
                if (audioFiles.Count == 1)
                    actionEvent.Sound = CreateSound(audioSettingsViewModel, audioFiles[0]);
                else
                {
                    actionEvent.RandomSequenceContainer = new RandomSequenceContainer
                    {
                        Sounds = [],
                        AudioSettings = audioSettingsViewModel.BuildAudioSettings()
                    };

                    foreach (var audioFile in audioFiles)
                    {
                        var sound = CreateSound(audioSettingsViewModel, audioFile);
                        actionEvent.RandomSequenceContainer.Sounds.Add(sound);
                    }
                }

                return actionEvent;
            }

            return null;
        }

        public static string GetActionEventNameFromDataGridRow(Dictionary<string, string> dataGridRow)
        {
            if (dataGridRow.TryGetValue("Event", out var eventName))
                return eventName.ToString();
            else
                return string.Empty;
        }

        public static StatePath GetStatePathFromDataGridRow(IAudioRepository audioRepository, Dictionary<string, string> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var dataGridRowStatePathNodes = GetStatePathNodes(audioRepository, dataGridRow, selectedDialogueEvent);

            foreach (var statePath in selectedDialogueEvent.StatePaths)
            {
                if (statePath.Nodes.SequenceEqual(dataGridRowStatePathNodes, new StatePathNodeComparer()))
                    return statePath;
            }

            return null;
        }

        public static List<StatePathNode> GetStatePathNodes(IAudioRepository audioRepository, Dictionary<string, string> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();
            foreach (var kvp in dataGridRow)
            {
                var columnName = RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });
            }

            return statePath.Nodes;
        }

        public static StatePath CreateStatePath(IAudioRepository audioRepository, AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, string> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();
            foreach (var kvp in dataGridRow)
            {
                var columnName = RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });

                var audioFiles = audioSettingsViewModel.AudioFiles;
                if (audioFiles.Count == 1)
                    statePath.Sound = CreateSound(audioSettingsViewModel, audioFiles[0]);
                else
                {
                    statePath.RandomSequenceContainer = new RandomSequenceContainer
                    {
                        Sounds = [],
                        AudioSettings = audioSettingsViewModel.BuildAudioSettings()
                    };

                    foreach (var audioFile in audioFiles)
                    {
                        var sound = CreateSound(audioSettingsViewModel, audioFile);
                        statePath.RandomSequenceContainer.Sounds.Add(sound);
                    }
                }
            }

            return statePath;
        }

        public class StatePathNodeComparer : IEqualityComparer<StatePathNode>
        {
            public bool Equals(StatePathNode x, StatePathNode y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                return string.Equals(x.StateGroup?.Name, y.StateGroup?.Name, StringComparison.Ordinal) &&
                       string.Equals(x.State?.Name, y.State?.Name, StringComparison.Ordinal);
            }

            public int GetHashCode(StatePathNode obj)
            {
                return HashCode.Combine(obj.StateGroup?.Name, obj.State?.Name);
            }
        }

        private static Sound CreateSound(AudioSettingsViewModel audioSettingsViewModel, AudioFile audioFile, bool isSoundInSoundContainer = false)
        {
            var sound = new Sound()
            {
                WavFileName = audioFile.FileName,
                WavFilePath = audioFile.FilePath,
            };

            return sound;
        }

        public static State GetStateFromDataGridRow(Dictionary<string, string> dataGridRow, StateGroup moddedStateGroup)
        {
            var dataGridRowState = CreateStateFromDataGridRow(dataGridRow);

            foreach (var state in moddedStateGroup.States)
            {                    
                if (state.Name == dataGridRowState.Name)
                    return state;
            }

            return null;
        }

        public static State CreateStateFromDataGridRow(Dictionary<string, string> dataGridRow)
        {
            var state = new State();
            state.Name = dataGridRow.First().Value.ToString();
            return state;
        }

        public static AudioSettings GetAudioSettingsFromAudioProjectViewerActionEventItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var audioProjectItem = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            var selectedAudioProjectViewerDataGridRow = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0];
            var soundBank = GetSoundBankFromName(audioProjectService, audioProjectItem.Name);
            var actionEvent = GetActionEventFromDataGridRow(selectedAudioProjectViewerDataGridRow, soundBank);

            // We could just not run this function unless we know the object has a random sequence container but we'll keep it as this as we may introduce more containers or enable settings for sounds in future.
            if (actionEvent.RandomSequenceContainer != null)
                return actionEvent.RandomSequenceContainer.AudioSettings;
            else
                return null;
        }

        public static AudioSettings GetAudioSettingsFromAudioProjectViewerStatePathItem(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var audioProjectItem = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            var selectedAudioProjectViewerDataGridRow = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows[0];
            var dialogueEvent = GetDialogueEventFromName(audioProjectService, audioProjectItem.Name);
            var statePath = GetStatePathFromDataGridRow(audioRepository, selectedAudioProjectViewerDataGridRow, dialogueEvent);

            // We could just not run this function unless we know the object has a random sequence container but we'll keep it as this as we may introduce more containers or enable settings for sounds in future.
            if (statePath.RandomSequenceContainer != null)
                return statePath.RandomSequenceContainer.AudioSettings;
            else
                return null;
        }

        public static void InsertDataGridRowAlphabetically(ObservableCollection<Dictionary<string, string>> audioProjectViewerDataGrid, Dictionary<string, string> newRow)
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
            var decisionTree = selectedDialogueEvent.StatePaths;
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
