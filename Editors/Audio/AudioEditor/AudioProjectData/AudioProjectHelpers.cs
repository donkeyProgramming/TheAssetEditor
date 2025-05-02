using System;
using System.Collections.Generic;
using System.Data;
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

        public static ActionEvent GetActionEventFromDataGridRow(DataRow row, SoundBank actionEventSoundBank)
        {
            var eventName = GetActionEventNameFromDataRow(row);

            foreach (var actionEvent in actionEventSoundBank.ActionEvents)
            {                    
                if (actionEvent.Name == eventName)
                    return actionEvent;
            }

            return null;
        }

        public static string GetActionEventNameFromDataRow(DataRow row)
        {
            if (row == null)
                return string.Empty;

            // Ensure the column exists in the row’s table (defensive check).
            if (!row.Table.Columns.Contains(DataGridConfiguration.EventNameColumn))
                return string.Empty;

            var value = row[DataGridConfiguration.EventNameColumn];
            return value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
        }

        public static ActionEvent CreateActionEventFromDataGridRow(AudioSettingsViewModel audioSettingsViewModel, DataRow row)
        {
            var actionEvent = new ActionEvent();

            actionEvent.Name = GetActionEventNameFromDataRow(row);

            var audioFiles = audioSettingsViewModel.AudioFiles;
            if (audioFiles.Count == 1)
            {
                actionEvent.Sound = CreateSound(audioFiles[0]);
                actionEvent.Sound.AudioSettings = BuildSoundSettings(audioSettingsViewModel);
            }
            else
            {
                actionEvent.RandomSequenceContainer = new RandomSequenceContainer
                {
                    Sounds = [],
                    AudioSettings = BuildRanSeqContainerSettings(audioSettingsViewModel)
                };

                foreach (var audioFile in audioFiles)
                {
                    var sound = CreateSound(audioFile);
                    actionEvent.RandomSequenceContainer.Sounds.Add(sound);
                }
            }

            return actionEvent;
        }

        public static StatePath GetStatePathFromDataGridRow(IAudioRepository audioRepository, DataRow row, DialogueEvent selectedDialogueEvent)
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
                    StateGroup = new StateGroup { Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName) },
                    State = new State { Name = columnValue }
                });
            }

            return statePath.Nodes;
        }

        public static StatePath CreateStatePathFromDataGridRow(IAudioRepository audioRepository, AudioSettingsViewModel audioSettingsViewModel, DataRow row, DialogueEvent selectedDialogueEvent)
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
                        Name = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, columnName)
                    },
                    State = new State { Name = value }
                });
            }

            var audioFiles = audioSettingsViewModel.AudioFiles;
            if (audioFiles.Count == 1)
            {
                statePath.Sound = CreateSound(audioFiles[0]);
                statePath.Sound.AudioSettings = BuildSoundSettings(audioSettingsViewModel);
            }
            else
            {
                statePath.RandomSequenceContainer = new RandomSequenceContainer
                {
                    Sounds = [],
                    AudioSettings = BuildRanSeqContainerSettings(audioSettingsViewModel)
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

        public static State GetStateFromDataGridRow(DataRow row, StateGroup moddedStateGroup)
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

        public static RanSeqContainerSettings BuildRecommendedRanSeqContainerSettings(List<AudioFile> audioFiles)
        {
            var audioSettings = new RanSeqContainerSettings();
            audioSettings.PlaylistType = PlaylistType.RandomExhaustive;
            audioSettings.EnableRepetitionInterval = true;
            audioSettings.RepetitionInterval = (uint)Math.Ceiling(audioFiles.Count / 2.0);
            audioSettings.EndBehaviour = EndBehaviour.Restart;
            audioSettings.AlwaysResetPlaylist = true;
            audioSettings.PlaylistMode = PlaylistMode.Step;
            audioSettings.LoopingType = LoopingType.Disabled;
            audioSettings.NumberOfLoops = 1;
            audioSettings.TransitionType = TransitionType.Disabled;
            audioSettings.TransitionDuration = 1;
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
