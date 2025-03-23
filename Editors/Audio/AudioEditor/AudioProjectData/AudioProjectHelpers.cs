using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public class AudioProjectHelpers
    {
        public static SoundBank GetSoundBankFromName(IAudioEditorService audioEditorService, string soundBankName)
        {
            return audioEditorService.AudioProject.SoundBanks
                .FirstOrDefault(soundBank => soundBank.Name == soundBankName);
        }

        public static DialogueEvent GetDialogueEventFromName(IAudioEditorService audioEditorService, string dialogueEventName)
        {
            return audioEditorService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.SoundBankType == SoundBanks.Wh3SoundBankType.DialogueEventSoundBank)
                .SelectMany(soundBank => soundBank.DialogueEvents)
                .FirstOrDefault(dialogueEvent => dialogueEvent.Name == dialogueEventName);
        }

        public static StateGroup GetStateGroupFromName(IAudioEditorService audioEditorService, string stateGroupName)
        {
            return audioEditorService.AudioProject.StateGroups
                .FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);
        }

        public static ActionEvent GetActionEventFromDataGridRow(Dictionary<string, string> dataGridRow, SoundBank actionEventSoundBank)
        {
            var eventName = GetActionEventNameWithoutActionTypeFromDataGridRow(dataGridRow);

            foreach (var actionEvent in actionEventSoundBank.ActionEvents)
            {                    
                if (actionEvent.Name == eventName)
                    return actionEvent;
            }

            return null;
        }

        public static string GetActionEventNameWithoutActionTypeFromDataGridRow(Dictionary<string, string> dataGridRow)
        {
            if (dataGridRow.TryGetValue(DataGridConfiguration.EventNameColumn, out var eventName))
                return eventName.ToString();
            else
                return string.Empty;
        }

        public static string GetActionTypeFromDataGridRow(Dictionary<string, string> dataGridRow)
        {
            if (dataGridRow.TryGetValue(DataGridConfiguration.ActionTypeColumn, out var actionType))
                return actionType.ToString();
            else
                return string.Empty;
        }

        public static string GetActionEventName(string actionType, string eventName)
        {
            return $"{actionType}_{eventName}";
        }

        public static ActionEvent CreateActionEventFromDataGridRow(AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, string> dataGridRow)
        {
            var actionEvent = new ActionEvent();

            var eventNameWithoutActionType = GetActionEventNameWithoutActionTypeFromDataGridRow(dataGridRow);
            var actionType = GetActionTypeFromDataGridRow(dataGridRow);

            if (eventNameWithoutActionType == string.Empty || actionType == string.Empty)
                return null;

            actionEvent.Name = GetActionEventName(actionType, eventNameWithoutActionType);

            var audioFiles = audioSettingsViewModel.AudioFiles;
            if (audioFiles.Count == 1)
                actionEvent.Sound = CreateSound(audioSettingsViewModel, audioFiles[0], isInContainer: false);
            else
            {
                actionEvent.RandomSequenceContainer = new RandomSequenceContainer
                {
                    Sounds = [],
                    AudioSettings = BuildRanSeqContainerSettings(audioSettingsViewModel)
                };

                foreach (var audioFile in audioFiles)
                {
                    var sound = CreateSound(audioSettingsViewModel, audioFile);
                    actionEvent.RandomSequenceContainer.Sounds.Add(sound);
                }
            }

            return actionEvent;
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
                var columnName = DataGridHelpers.RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });
            }

            return statePath.Nodes;
        }

        public static StatePath CreateStatePathFromDataGridRow(IAudioRepository audioRepository, AudioSettingsViewModel audioSettingsViewModel, Dictionary<string, string> dataGridRow, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();
            foreach (var kvp in dataGridRow)
            {
                var columnName = DataGridHelpers.RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });

                var audioFiles = audioSettingsViewModel.AudioFiles;
                if (audioFiles.Count == 1)
                    statePath.Sound = CreateSound(audioSettingsViewModel, audioFiles[0], isInContainer: false);
                else
                {
                    statePath.RandomSequenceContainer = new RandomSequenceContainer
                    {
                        Sounds = [],
                        AudioSettings = BuildRanSeqContainerSettings(audioSettingsViewModel)
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

        private static Sound CreateSound(AudioSettingsViewModel audioSettingsViewModel, AudioFile audioFile, bool isInContainer = true)
        {
            var sound = new Sound()
            {
                WavFileName = audioFile.FileName,
                WavFilePath = audioFile.FilePath,
            };

            if (!isInContainer)
                sound.AudioSettings = BuildSoundSettings(audioSettingsViewModel);

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

        public static IAudioSettings GetAudioSettingsFromAudioProjectViewerActionEvent(IAudioEditorService audioEditorService)
        {
            var selectedNode = audioEditorService.GetSelectedExplorerNode();
            var selectedAudioProjectViewerDataGridRow = audioEditorService.GetSelectedViewerRows()[0];
            var soundBank = GetSoundBankFromName(audioEditorService, selectedNode.Name);
            var actionEvent = GetActionEventFromDataGridRow(selectedAudioProjectViewerDataGridRow, soundBank);

            if (actionEvent.RandomSequenceContainer != null)
                return actionEvent.RandomSequenceContainer.AudioSettings;
            else
                return actionEvent.Sound.AudioSettings;
        }

        public static IAudioSettings GetAudioSettingsFromAudioProjectViewerStatePath(AudioEditorViewModel audioEditorViewModel, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            var audioProjectItem = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            var selectedAudioProjectViewerDataGridRow = audioEditorService.GetSelectedViewerRows()[0];
            var dialogueEvent = GetDialogueEventFromName(audioEditorService, audioProjectItem.Name);
            var statePath = GetStatePathFromDataGridRow(audioRepository, selectedAudioProjectViewerDataGridRow, dialogueEvent);

            if (statePath.RandomSequenceContainer != null)
                return statePath.RandomSequenceContainer.AudioSettings;
            else
                return statePath.Sound.AudioSettings;
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

        public static RanSeqContainerSettings BuildRanSeqContainerSettings(AudioSettingsViewModel audioSettingsViewModel)
        {
            var audioSettings = new RanSeqContainerSettings();

            if (audioSettingsViewModel.AudioFiles.Count > 1)
            {
                audioSettings.PlaylistType = audioSettingsViewModel.PlaylistType;

                if (audioSettingsViewModel.PlaylistType == PlaylistType.Sequence)
                    audioSettings.EndBehaviour = audioSettingsViewModel.EndBehaviour;
                else
                {
                    audioSettings.EnableRepetitionInterval = audioSettingsViewModel.EnableRepetitionInterval;

                    if (audioSettingsViewModel.EnableRepetitionInterval)
                        audioSettings.RepetitionInterval = audioSettingsViewModel.RepetitionInterval;
                }

                audioSettings.AlwaysResetPlaylist = audioSettingsViewModel.AlwaysResetPlaylist;

                audioSettings.PlaylistMode = audioSettingsViewModel.PlaylistMode;
                audioSettings.LoopingType = audioSettingsViewModel.LoopingType;

                if (audioSettingsViewModel.LoopingType == LoopingType.FiniteLooping)
                    audioSettings.NumberOfLoops = audioSettingsViewModel.NumberOfLoops;

                if (audioSettingsViewModel.TransitionType != TransitionType.Disabled)
                {
                    audioSettings.TransitionType = audioSettingsViewModel.TransitionType;
                    audioSettings.TransitionDuration = audioSettingsViewModel.TransitionDuration;
                }
            }

            return audioSettings;
        }

        public static SoundSettings BuildSoundSettings(AudioSettingsViewModel audioSettingsViewModel)
        {
            var audioSettings = new SoundSettings();

            audioSettings.LoopingType = audioSettingsViewModel.LoopingType;
            if (audioSettingsViewModel.LoopingType == LoopingType.FiniteLooping)
                audioSettings.NumberOfLoops = audioSettingsViewModel.NumberOfLoops;

            return audioSettings;
        }
    }
}
