using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectHelpers
    {
        public static string GetStateGroupFromStateGroupWithQualifier(IAudioRepository audioRepository, string dialogueEvent, string stateGroupWithQualifier)
        {
            if (audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent.TryGetValue(dialogueEvent, out var stateGroupDictionary))
                if (stateGroupDictionary.TryGetValue(stateGroupWithQualifier, out var stateGroup))
                    return stateGroup;

            return null;
        }

        public static SoundBank GetSoundBankFromName(AudioProject audioProject, string soundBankName)
        {
            return audioProject.SoundBanks
                .FirstOrDefault(soundBank => soundBank.Name == soundBankName);
        }

        public static DialogueEvent GetDialogueEventFromName(AudioProject audioProject, string dialogueEventName)
        {
            return audioProject.SoundBanks
                .Where(soundBank => soundBank.SoundBankType == SoundBanks.Wh3SoundBankType.DialogueEventSoundBank)
                .SelectMany(soundBank => soundBank.DialogueEvents)
                .FirstOrDefault(dialogueEvent => dialogueEvent.Name == dialogueEventName);
        }

        public static StateGroup GetStateGroupFromName(AudioProject audioProject, string stateGroupName)
        {
            return audioProject.StateGroups
                .FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);
        }

        public static ActionEvent GetActionEventFromRow(DataRow row, SoundBank actionEventSoundBank)
        {
            var eventName = GetActionEventNameFromRow(row);

            foreach (var actionEvent in actionEventSoundBank.ActionEvents)
            {
                if (actionEvent.Name == eventName)
                    return actionEvent;
            }

            return null;
        }

        public static ActionEvent CreateActionEventFromRow(ObservableCollection<AudioFile> audioFiles, IAudioSettings audioSettings, DataRow row)
        {
            var actionEvent = new ActionEvent();

            actionEvent.Name = GetActionEventNameFromRow(row);

            if (audioFiles.Count == 1)
            {
                var storedSoundSettings = audioSettings as SoundSettings;
                actionEvent.Sound = CreateSound(audioFiles[0]);
                actionEvent.Sound.AudioSettings = BuildSoundSettings(storedSoundSettings);
            }
            else
            {
                var storedRanSeqContainerSettings = audioSettings as RanSeqContainerSettings;
                actionEvent.RandomSequenceContainer = new RandomSequenceContainer
                {
                    Sounds = [],
                    AudioSettings = BuildRanSeqContainerSettings(storedRanSeqContainerSettings)
                };

                foreach (var audioFile in audioFiles)
                {
                    var sound = CreateSound(audioFile);
                    actionEvent.RandomSequenceContainer.Sounds.Add(sound);
                }
            }

            return actionEvent;
        }

        public static string GetActionEventNameFromRow(DataRow row)
        {
            return row[DataGridTemplates.EventNameColumn].ToString();
        }

        public static StatePath GetStatePathFromRow(IAudioRepository audioRepository, DataRow row, DialogueEvent selectedDialogueEvent)
        {
            // Remove any rows with empty values due to a new CA state group being added to still allow the user to edit the out of date state path
            var filteredRow = row.Table.Columns
                .Cast<DataColumn>()
                .Where(column =>
                {
                    var cell = row[column];
                    return cell != DBNull.Value && !string.IsNullOrEmpty(cell.ToString());
                })
                .ToDictionary(column => column.ColumnName, column => row[column].ToString());

            var dataGridRowStatePathNodes = GetStatePathNodes(audioRepository, filteredRow, selectedDialogueEvent);

            foreach (var statePath in selectedDialogueEvent.StatePaths)
            {
                if (statePath.Nodes.SequenceEqual(dataGridRowStatePathNodes, new StatePathNodeComparer()))
                    return statePath;
            }

            return null;
        }

        public static List<StatePathNode> GetStatePathNodes(IAudioRepository audioRepository, Dictionary<string, string> row, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();
            foreach (var kvp in row)
            {
                var columnName = DataGridHelpers.RemoveExtraUnderscoresFromString(kvp.Key);
                var columnValue = kvp.Value;
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = GetStateGroupFromStateGroupWithQualifier(audioRepository, selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });
            }

            return statePath.Nodes;
        }

        public static StatePath CreateStatePathFromRow(IAudioRepository audioRepository, ObservableCollection<AudioFile> audioFiles, IAudioSettings audioSettings, DataRow row, DialogueEvent selectedDialogueEvent)
        {
            var statePath = new StatePath();

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column].ToString();
                var columnName = DataGridHelpers.RemoveExtraUnderscoresFromString(column.ColumnName);

                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup
                    {
                        Name = GetStateGroupFromStateGroupWithQualifier(audioRepository, selectedDialogueEvent.Name, columnName)
                    },
                    State = new State { Name = value }
                });
            }

            if (audioFiles.Count == 1)
            {
                var storedSoundSettings = audioSettings as SoundSettings;
                statePath.Sound = CreateSound(audioFiles[0]);
                statePath.Sound.AudioSettings = BuildSoundSettings(storedSoundSettings);
            }
            else
            {
                var storedRanSeqContainerSettings = audioSettings as RanSeqContainerSettings;
                statePath.RandomSequenceContainer = new RandomSequenceContainer
                {
                    Sounds = [],
                    AudioSettings = BuildRanSeqContainerSettings(storedRanSeqContainerSettings)
                };

                foreach (var audioFile in audioFiles)
                    statePath.RandomSequenceContainer.Sounds.Add(CreateSound(audioFile));
            }

            return statePath;
        }

        public static StatePath CreateStatePathFromStatePathNodes(IAudioRepository audioRepository, List<StatePathNode> statePathNodes, List<AudioFile> audioFiles)
        {
            var statePath = new StatePath();

            foreach (var statePathNode in statePathNodes)
            {
                statePath.Nodes.Add(new StatePathNode
                {
                    StateGroup = new StateGroup { Name = statePathNode.StateGroup.Name },
                    State = new State { Name = statePathNode.State.Name }
                });

                if (audioFiles.Count == 1)
                {
                    statePath.Sound = CreateSound(audioFiles[0]);
                    statePath.Sound.AudioSettings = new SoundSettings();
                }
                else
                {
                    statePath.RandomSequenceContainer = new RandomSequenceContainer
                    {
                        Sounds = [],
                        AudioSettings = BuildRecommendedRanSeqContainerSettings(audioFiles)
                    };

                    foreach (var audioFile in audioFiles)
                    {
                        var sound = CreateSound(audioFile);
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

        private static Sound CreateSound(AudioFile audioFile)
        {
            var sound = new Sound()
            {
                WavFileName = audioFile.FileName,
                WavFilePath = audioFile.FilePath,
            };

            return sound;
        }

        public static State GetStateFromRow(DataRow row, StateGroup moddedStateGroup)
        {
            var dataRowState = CreateStateFromDataGridRow(row);
            return moddedStateGroup.States.FirstOrDefault(state => state.Name == dataRowState.Name);
        }

        public static State CreateStateFromDataGridRow(DataRow row)
        {
            var state = new State();

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column];

                if (value != DBNull.Value && !string.IsNullOrEmpty(value.ToString()))
                {
                    state.Name = value.ToString();
                    break;
                }
            }

            return state;
        }

        public static IAudioSettings GetAudioSettingsFromActionEvent(IAudioEditorService audioEditorService)
        {
            var selectedNode = audioEditorService.SelectedExplorerNode;
            var selectedAudioProjectViewerDataGridRow = audioEditorService.GetSelectedViewerRows()[0];
            var soundBank = GetSoundBankFromName(audioEditorService.AudioProject, selectedNode.Name);
            var actionEvent = GetActionEventFromRow(selectedAudioProjectViewerDataGridRow, soundBank);

            if (actionEvent.RandomSequenceContainer != null)
                return actionEvent.RandomSequenceContainer.AudioSettings;
            else
                return actionEvent.Sound.AudioSettings;
        }

        public static IAudioSettings GetAudioSettingsFromStatePath(AudioEditorViewModel audioEditorViewModel, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            var audioProjectItem = audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode;
            var selectedAudioProjectViewerDataGridRow = audioEditorService.GetSelectedViewerRows()[0];
            var dialogueEvent = GetDialogueEventFromName(audioEditorService.AudioProject, audioProjectItem.Name);
            var statePath = GetStatePathFromRow(audioRepository, selectedAudioProjectViewerDataGridRow, dialogueEvent);

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

        public static SoundSettings BuildSoundSettings(SoundSettings storedSoundSettings)
        {
            var soundSettings = new SoundSettings();
            soundSettings.LoopingType = storedSoundSettings.LoopingType;
            if (storedSoundSettings.LoopingType == LoopingType.FiniteLooping)
                soundSettings.NumberOfLoops = storedSoundSettings.NumberOfLoops;
            return soundSettings;
        }

        public static RanSeqContainerSettings BuildRanSeqContainerSettings(RanSeqContainerSettings storedRanSeqContainerSettings)
        {
            var ranSeqContainerSettings = new RanSeqContainerSettings();

            ranSeqContainerSettings.PlaylistType = storedRanSeqContainerSettings.PlaylistType;

            if (storedRanSeqContainerSettings.PlaylistType == PlaylistType.Sequence)
                ranSeqContainerSettings.EndBehaviour = storedRanSeqContainerSettings.EndBehaviour;
            else
            {
                ranSeqContainerSettings.EnableRepetitionInterval = storedRanSeqContainerSettings.EnableRepetitionInterval;

                if (storedRanSeqContainerSettings.EnableRepetitionInterval)
                    ranSeqContainerSettings.RepetitionInterval = storedRanSeqContainerSettings.RepetitionInterval;
            }

            ranSeqContainerSettings.AlwaysResetPlaylist = storedRanSeqContainerSettings.AlwaysResetPlaylist;

            ranSeqContainerSettings.PlaylistMode = storedRanSeqContainerSettings.PlaylistMode;
            ranSeqContainerSettings.LoopingType = storedRanSeqContainerSettings.LoopingType;

            if (storedRanSeqContainerSettings.LoopingType == LoopingType.FiniteLooping)
                ranSeqContainerSettings.NumberOfLoops = storedRanSeqContainerSettings.NumberOfLoops;

            if (storedRanSeqContainerSettings.TransitionType != TransitionType.Disabled)
            {
                ranSeqContainerSettings.TransitionType = storedRanSeqContainerSettings.TransitionType;
                ranSeqContainerSettings.TransitionDuration = storedRanSeqContainerSettings.TransitionDuration;
            }

            return ranSeqContainerSettings;
        }

        public static RanSeqContainerSettings BuildRecommendedRanSeqContainerSettings(List<AudioFile> audioFiles)
        {
            var ranSeqContainerSettings = new RanSeqContainerSettings();
            ranSeqContainerSettings.PlaylistType = PlaylistType.RandomExhaustive;
            ranSeqContainerSettings.EnableRepetitionInterval = true;
            ranSeqContainerSettings.RepetitionInterval = (uint)Math.Ceiling(audioFiles.Count / 2.0);
            ranSeqContainerSettings.EndBehaviour = EndBehaviour.Restart;
            ranSeqContainerSettings.AlwaysResetPlaylist = true;
            ranSeqContainerSettings.PlaylistMode = PlaylistMode.Step;
            ranSeqContainerSettings.LoopingType = LoopingType.Disabled;
            ranSeqContainerSettings.NumberOfLoops = 1;
            ranSeqContainerSettings.TransitionType = TransitionType.Disabled;
            ranSeqContainerSettings.TransitionDuration = 1;
            return ranSeqContainerSettings;
        }
    }
}
