using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public class TreeBuilder
    {
        public static void CreateAudioProjectTree(IAudioEditorService audioEditorService, ObservableCollection<TreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            audioProjectTree.Clear();
            AddActionEvents(audioEditorService, audioProjectTree, showEditedAudioProjectItemsOnly);
            AddDialogueEvents(audioEditorService, audioProjectTree, showEditedAudioProjectItemsOnly);
            AddStateGroups(audioEditorService, audioProjectTree, showEditedAudioProjectItemsOnly);
        }

        private static void AddActionEvents(IAudioEditorService audioEditorService, ObservableCollection<TreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var soundBanksNode = new TreeNode
            {
                Name = "Action Events",
                NodeType = NodeType.ActionEventsContainer,
                Children = new ObservableCollection<TreeNode>()
            };
            audioProjectTree.Add(soundBanksNode);

            AddActionEventSoundBankNodes(audioEditorService, soundBanksNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddDialogueEvents(IAudioEditorService audioEditorService, ObservableCollection<TreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var dialogueEventsNode = new TreeNode
            {
                Name = "Dialogue Events",
                NodeType = NodeType.DialogueEventsContainer,
                Children = new ObservableCollection<TreeNode>()
            };
            audioProjectTree.Add(dialogueEventsNode);

            AddDialogueEventSoundBankNodes(audioEditorService, dialogueEventsNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddStateGroups(IAudioEditorService audioEditorService, ObservableCollection<TreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            var stateGroupsNode = new TreeNode
            {
                Name = "State Groups",
                NodeType = NodeType.StateGroupsContainer,
                Children = new ObservableCollection<TreeNode>()
            };
            audioProjectTree.Add(stateGroupsNode);

            AddStateGroupNodes(audioEditorService, stateGroupsNode, showEditedAudioProjectItemsOnly);
        }

        private static void AddActionEventSoundBankNodes(IAudioEditorService audioEditorService, TreeNode soundBanksNode, bool showEditedAudioProjectItemsOnly)
        {
            var actionEventSoundBanks = GetSoundBanks(audioEditorService, showEditedAudioProjectItemsOnly, Wh3SoundBankType.ActionEventSoundBank);
            if (actionEventSoundBanks.Count > 0)
            {
                foreach (var soundBank in actionEventSoundBanks)
                {
                    var soundBankNode = new TreeNode
                    {
                        Name = soundBank.Name,
                        NodeType = NodeType.ActionEventSoundBank,
                        Parent = soundBanksNode
                    };
                    soundBanksNode.Children.Add(soundBankNode);
                }
            }
        }

        private static void AddDialogueEventSoundBankNodes(IAudioEditorService audioEditorService, TreeNode soundBanksNode, bool showEditedAudioProjectItemsOnly)
        {
            var dialogueEventSoundBanks = GetSoundBanks(audioEditorService, showEditedAudioProjectItemsOnly, Wh3SoundBankType.DialogueEventSoundBank);
            if (dialogueEventSoundBanks.Count > 0)
            {
                foreach (var soundBank in  dialogueEventSoundBanks)
                {
                    var soundBankNode = new TreeNode
                    {
                        Name = soundBank.Name,
                        NodeType = NodeType.DialogueEventSoundBank,
                        Parent = soundBanksNode,
                        Children = new ObservableCollection<TreeNode>()
                    };

                    AddDialogueEventNodes(showEditedAudioProjectItemsOnly, soundBank, soundBankNode);

                    soundBanksNode.Children.Add(soundBankNode);
                }
            }
        }

        private static void AddDialogueEventNodes(bool showEditedAudioProjectItemsOnly, SoundBank soundBank, TreeNode soundBankNode)
        {
            var dialogueEvents = showEditedAudioProjectItemsOnly
                ? soundBank.DialogueEvents.Where(dialogueEvent => dialogueEvent.StatePaths.Count > 0)
                : soundBank.DialogueEvents;

            foreach (var dialogueEvent in dialogueEvents)
            {
                var dialogueEventNode = new TreeNode
                {
                    Name = dialogueEvent.Name,
                    NodeType = NodeType.DialogueEvent,
                    Parent = soundBankNode
                };
                soundBankNode.Children.Add(dialogueEventNode);
            }
        }

        private static void AddStateGroupNodes(IAudioEditorService audioEditorService, TreeNode stateGroupsNode, bool showEditedAudioProjectItemsOnly)
        {
            var stateGroups = GetStates(audioEditorService, showEditedAudioProjectItemsOnly);
            if (stateGroups.Count > 0)
            {
                foreach (var stateGroup in stateGroups)
                {
                    var stateGroupNode = new TreeNode
                    {
                        Name = stateGroup.Name,
                        NodeType = NodeType.StateGroup,
                        Parent = stateGroupsNode
                    };
                    stateGroupsNode.Children.Add(stateGroupNode);
                }
            }
        }

        public static void AddFilteredDialogueEventsToSoundBankTreeViewItems(IAudioEditorService audioEditorService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, string soundBankName, DialogueEventPreset? dialogueEventPreset)
        {
            var filteredDialogueEventNames = DialogueEventData
                .Where(dialogueEvent => GetSoundBankSubTypeString(dialogueEvent.SoundBank) == audioEditorService.SelectedExplorerNode.Name
                && (!dialogueEventPreset.HasValue || dialogueEvent.DialogueEventPreset.Contains(dialogueEventPreset.Value)))
                .Select(dialogueEvent => dialogueEvent.Name)
                .ToHashSet();

            var soundBank = TreeNode.GetAudioProjectTreeNodeFromName(audioProjectExplorerViewModel.AudioProjectTree, soundBankName);

            foreach (var dialogueEvent in soundBank.Children)
            {
                if (filteredDialogueEventNames.Contains(dialogueEvent.Name))
                    dialogueEvent.IsVisible = true;
                else
                    dialogueEvent.IsVisible = false;
            }
        }

        public static void FilterEditedAudioProjectItems(IAudioEditorService audioEditorService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, ObservableCollection<TreeNode> audioProjectTree, bool showEditedAudioProjectItemsOnly)
        {
            FilterEditedAudioProjectItemsInner(audioEditorService, audioProjectExplorerViewModel, audioProjectTree, showEditedAudioProjectItemsOnly);

            if (!showEditedAudioProjectItemsOnly)
            {
                var dialogueEventsNode = TreeNode.GetAudioProjectTreeNodeFromName(audioProjectTree, "Dialogue Events");

                if (dialogueEventsNode == null)
                    return;

                var dialogueEventSoundBanks = dialogueEventsNode.Children;

                foreach (var soundBank in dialogueEventSoundBanks)
                {
                    if (soundBank.PresetFilter != null && soundBank.PresetFilter != DialogueEventPreset.ShowAll)
                    {
                        var filteredDialogueEventNames = DialogueEventData
                            .Where(dialogueEvent => GetSoundBankSubTypeString(dialogueEvent.SoundBank) == soundBank.Name
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

        public static void FilterEditedAudioProjectItemsInner(IAudioEditorService audioEditorService, AudioProjectExplorerViewModel audioProjectExplorerViewModel, ObservableCollection<TreeNode> node, bool showEditedAudioProjectItemsOnly)
        {
            foreach (var childNode in node)
            {
                if (childNode.Children.Any())
                {
                    FilterEditedAudioProjectItemsInner(audioEditorService, audioProjectExplorerViewModel, childNode.Children, showEditedAudioProjectItemsOnly);

                    if (!childNode.Children.Any(childNode => childNode.IsVisible))
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
                    case NodeType.StateGroupsContainer:
                        childNode.IsVisible = childNode.Children.Any();
                        break;

                    case NodeType.ActionEventSoundBank:
                        childNode.IsVisible = audioEditorService.AudioProject.SoundBanks.Any(soundBank => soundBank.Name == childNode.Name && soundBank.ActionEvents.Count > 0);
                        break;

                    case NodeType.DialogueEventSoundBank:
                        childNode.IsVisible = audioEditorService.AudioProject.SoundBanks.Any(soundBank => soundBank.Name == childNode.Name && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.StatePaths.Count > 0));
                        break;

                    case NodeType.StateGroup:
                        childNode.IsVisible = audioEditorService.AudioProject.StateGroups.Any(stateGroup => stateGroup.Name == childNode.Name && stateGroup.States.Count > 0);
                        break;

                    case NodeType.DialogueEvent:
                        childNode.IsVisible = audioEditorService.AudioProject.SoundBanks.Any(soundBank => soundBank.Name == childNode.Parent.Name && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.Name == childNode.Name && dialogueEvent.StatePaths.Count > 0));
                        break;

                    default:
                        childNode.IsVisible = true;
                        break;
                }
            }

            if (!node.Any(node => node.IsVisible))
            {
                foreach (var parentNode in node)
                    parentNode.IsVisible = false;
            }
        }

        private static List<SoundBank> GetSoundBanks(IAudioEditorService audioEditorService, bool showEditedAudioProjectItemsOnly, Wh3SoundBankType gameSoundBankType)
        {
            if (showEditedAudioProjectItemsOnly)
            {
                if (gameSoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                {
                    return audioEditorService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank
                        && soundBank.ActionEvents.Count > 0)
                        .ToList();
                }
                else
                {
                    return audioEditorService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.SoundBankType == Wh3SoundBankType.DialogueEventSoundBank
                        && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.StatePaths.Count > 0))
                        .ToList();
                }
            }
            else
            {
                if (gameSoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                {
                    return audioEditorService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                        .ToList();
                }
                else
                {
                    return audioEditorService.AudioProject.SoundBanks
                        .Where(soundBank => soundBank.SoundBankType == Wh3SoundBankType.DialogueEventSoundBank)
                        .ToList();
                }
            }

        }

        private static List<StateGroup> GetStates(IAudioEditorService audioEditorService, bool showEditedAudioProjectItemsOnly)
        {
            if (showEditedAudioProjectItemsOnly)
            {
                return audioEditorService.AudioProject.StateGroups
                    .Where(state => state.States.Count > 0)
                    .ToList();
            }
            else
            {
                return audioEditorService.AudioProject.StateGroups
                    .ToList();
            }
        }
    }
}
