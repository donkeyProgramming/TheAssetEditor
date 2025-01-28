using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class AudioProjectTreeBuilder
    {
        public static void CreateAudioProjectTree(IAudioProjectService audioProjectService, ObservableCollection<AudioProjectTreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            audioProjectTree.Clear();
            AddActionEvents(audioProjectService, audioProjectTree, showEditedAudioProjectItemsOnly);
            AddDialogueEvents(audioProjectService, audioProjectTree, showEditedAudioProjectItemsOnly);
            AddStateGroups(audioProjectService, audioProjectTree, showEditedAudioProjectItemsOnly);
        }

        private static void AddActionEvents(IAudioProjectService audioProjectService, ObservableCollection<AudioProjectTreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var soundBanksNode = new AudioProjectTreeNode
            {
                Name = "Action Events",
                NodeType = NodeType.ActionEvents,
                Children = new ObservableCollection<AudioProjectTreeNode>()
            };
            audioProjectTree.Add(soundBanksNode);

            AddActionEventSoundBankNodes(audioProjectService, soundBanksNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddDialogueEvents(IAudioProjectService audioProjectService, ObservableCollection<AudioProjectTreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var dialogueEventsNode = new AudioProjectTreeNode
            {
                Name = "Dialogue Events",
                NodeType = NodeType.DialogueEvents,
                Children = new ObservableCollection<AudioProjectTreeNode>()
            };
            audioProjectTree.Add(dialogueEventsNode);

            AddDialogueEventSoundBankNodes(audioProjectService, dialogueEventsNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddStateGroups(IAudioProjectService audioProjectService, ObservableCollection<AudioProjectTreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var stateGroupsNode = new AudioProjectTreeNode
            {
                Name = "State Groups",
                NodeType = NodeType.StateGroups,
                Children = new ObservableCollection<AudioProjectTreeNode>()
            };
            audioProjectTree.Add(stateGroupsNode);

            AddStateGroupNodes(audioProjectService, stateGroupsNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddActionEventSoundBankNodes(IAudioProjectService audioProjectService, AudioProjectTreeNode soundBanksNode, bool showEditedAudioProjectItemsOnly)
        {
            var actionEventSoundBanks = GetSoundBanks(audioProjectService, showEditedAudioProjectItemsOnly, Wh3SoundBankType.ActionEventSoundBank);
            if (actionEventSoundBanks.Count > 0)
            {
                foreach (var soundBank in actionEventSoundBanks)
                {
                    var soundBankNode = new AudioProjectTreeNode
                    {
                        Name = soundBank.Name,
                        NodeType = NodeType.ActionEventSoundBank,
                        Parent = soundBanksNode
                    };
                    soundBanksNode.Children.Add(soundBankNode);
                }
            }
        }

        private static void AddDialogueEventSoundBankNodes(IAudioProjectService audioProjectService, AudioProjectTreeNode soundBanksNode, bool showEditedAudioProjectItemsOnly)
        {
            var dialogueEventSoundBanks = GetSoundBanks(audioProjectService, showEditedAudioProjectItemsOnly, Wh3SoundBankType.DialogueEventSoundBank);
            if (dialogueEventSoundBanks.Count > 0)
            {
                foreach (var soundBank in  dialogueEventSoundBanks)
                {
                    var soundBankNode = new AudioProjectTreeNode
                    {
                        Name = soundBank.Name,
                        NodeType = NodeType.DialogueEventSoundBank,
                        Parent = soundBanksNode,
                        Children = new ObservableCollection<AudioProjectTreeNode>()
                    };

                    AddDialogueEventNodes(showEditedAudioProjectItemsOnly, soundBank, soundBankNode);

                    soundBanksNode.Children.Add(soundBankNode);
                }
            }
        }

        private static void AddDialogueEventNodes(bool showEditedAudioProjectItemsOnly, SoundBank soundBank, AudioProjectTreeNode soundBankNode)
        {
            var dialogueEvents = showEditedAudioProjectItemsOnly
                ? soundBank.DialogueEvents.Where(dialogueEvent => dialogueEvent.DecisionTree.Count > 0)
                : soundBank.DialogueEvents;

            foreach (var dialogueEvent in dialogueEvents)
            {
                var dialogueEventNode = new AudioProjectTreeNode
                {
                    Name = dialogueEvent.Name,
                    NodeType = NodeType.DialogueEvent,
                    Parent = soundBankNode
                };
                soundBankNode.Children.Add(dialogueEventNode);
            }
        }

        private static void AddStateGroupNodes(IAudioProjectService audioProjectService, AudioProjectTreeNode stateGroupsNode, bool showEditedAudioProjectItemsOnly)
        {
            var stateGroups = GetStates(audioProjectService, showEditedAudioProjectItemsOnly);
            if (stateGroups.Count > 0)
            {
                foreach (var stateGroup in stateGroups)
                {
                    var stateGroupNode = new AudioProjectTreeNode
                    {
                        Name = stateGroup.Name,
                        NodeType = NodeType.StateGroup,
                        Parent = stateGroupsNode
                    };
                    stateGroupsNode.Children.Add(stateGroupNode);
                }
            }
        }

        public static void AddFilteredDialogueEventsToSoundBankTreeViewItems(IAudioProjectService audioProjectService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, string soundBankName, DialogueEventPreset? dialogueEventPreset)
        {
            var filteredDialogueEventNames = DialogueEventData
                .Where(dialogueEvent => GetSoundBankDisplayString(dialogueEvent.SoundBank) == audioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name 
                && (!dialogueEventPreset.HasValue || dialogueEvent.DialogueEventPreset.Contains(dialogueEventPreset.Value)))
                .Select(dialogueEvent => dialogueEvent.Name)
                .ToHashSet();

            var soundBank = AudioProjectHelpers.GetAudioProjectTreeNodeFromName(audioProjectExplorerViewModel.AudioProjectTree, soundBankName);

            foreach (var dialogueEvent in soundBank.Children)
            {
                if (filteredDialogueEventNames.Contains(dialogueEvent.Name))
                    dialogueEvent.IsVisible = true;
                else
                    dialogueEvent.IsVisible = false;
            }
        }

        public static void FilterEditedAudioProjectItems(IAudioProjectService audioProjectService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, ObservableCollection<AudioProjectTreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            FilterEditedAudioProjectItemsInner(audioProjectService, audioProjectExplorerViewModel, audioProjectTree, showEditedAudioProjectItemsOnly);

            if (!showEditedAudioProjectItemsOnly)
            {
                var dialogueEventsNode = AudioProjectHelpers.GetAudioProjectTreeNodeFromName(audioProjectTree, "Dialogue Events");
                var dialogueEventSoundBanks = dialogueEventsNode.Children;

                foreach (var soundBank in dialogueEventSoundBanks)
                {
                    if (soundBank.PresetFilter != null && soundBank.PresetFilter != DialogueEventPreset.ShowAll)
                    {
                        var filteredDialogueEventNames = DialogueEventData
                            .Where(dialogueEvent => GetSoundBankDisplayString(dialogueEvent.SoundBank) == soundBank.Name
                            && (!soundBank.PresetFilter.HasValue || dialogueEvent.DialogueEventPreset.Contains(soundBank.PresetFilter.Value)))
                            .Select(dialogueEvent => dialogueEvent.Name)
                            .ToHashSet();

                        foreach (var dialogueEvent in soundBank.Children)
                        {
                            if (filteredDialogueEventNames.Contains(dialogueEvent.Name))
                                dialogueEvent.IsVisible = true;
                            else
                                dialogueEvent.IsVisible = false;
                        }
                    }
                }
            }
        }

        public static void FilterEditedAudioProjectItemsInner(IAudioProjectService audioProjectService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, ObservableCollection<AudioProjectTreeNode> node, bool showEditedAudioProjectItemsOnly)
        {
            foreach (var childNode in node)
            {
                if (childNode.Children.Any())
                {
                    FilterEditedAudioProjectItemsInner(audioProjectService, audioProjectExplorerViewModel, childNode.Children, showEditedAudioProjectItemsOnly);

                    if (!childNode.Children.Any(c => c.IsVisible))
                    {
                        childNode.IsVisible = false;
                        continue;
                    }
                }

                if (!showEditedAudioProjectItemsOnly)
                {
                    childNode.IsVisible = true;
                    continue;
                }

                switch (childNode.NodeType)
                {
                    case NodeType.ActionEvents:
                    case NodeType.DialogueEvents:
                    case NodeType.StateGroups:
                        childNode.IsVisible = childNode.Children.Any();
                        break;

                    case NodeType.ActionEventSoundBank:
                        childNode.IsVisible = audioProjectService.AudioProject.SoundBanks.Any(soundBank => soundBank.Name == childNode.Name && soundBank.ActionEvents.Count > 0);
                        break;

                    case NodeType.DialogueEventSoundBank:
                        childNode.IsVisible = audioProjectService.AudioProject.SoundBanks.Any(soundBank => soundBank.Name == childNode.Name && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.DecisionTree.Count > 0));
                        break;

                    case NodeType.StateGroup:
                        childNode.IsVisible = audioProjectService.AudioProject.StateGroups.Any(stateGroup => stateGroup.Name == childNode.Name && stateGroup.States.Count > 0);
                        break;

                    default:
                        childNode.IsVisible = true;
                        break;
                }
            }

            if (!node.Any(n => n.IsVisible))
            {
                foreach (var parentNode in node)
                {
                    parentNode.IsVisible = false;
                }
            }
        }

        private static List<SoundBank> GetSoundBanks(IAudioProjectService audioProjectService, bool showEditedAudioProjectItemsOnly, Wh3SoundBankType gameSoundBankType)
        {
            if (showEditedAudioProjectItemsOnly)
            {
                if (gameSoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                {
                    return audioProjectService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.Type == Wh3SoundBankType.ActionEventSoundBank
                        && soundBank.ActionEvents.Count > 0)
                        .ToList();
                }
                else
                {
                    return audioProjectService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.Type == Wh3SoundBankType.DialogueEventSoundBank
                        && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.DecisionTree.Count > 0))
                        .ToList();
                }
            }
            else
            {
                if (gameSoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                {
                    return audioProjectService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
                        .ToList();
                }
                else
                {
                    return audioProjectService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.Type == Wh3SoundBankType.DialogueEventSoundBank)
                        .ToList();
                }
            }

        }

        private static List<StateGroup> GetStates(IAudioProjectService audioProjectService, bool showEditedAudioProjectItemsOnly)
        {
            if (showEditedAudioProjectItemsOnly)
            {
                return audioProjectService.AudioProject.StateGroups
                    .Where(state => state.States.Count > 0)
                    .ToList();
            }
            else
            {
                return audioProjectService.AudioProject.StateGroups
                    .ToList();
            }
        }
    }
}
