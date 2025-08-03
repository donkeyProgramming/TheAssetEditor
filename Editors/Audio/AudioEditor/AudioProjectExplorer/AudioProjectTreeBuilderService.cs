using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.Models;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public interface IAudioProjectTreeBuilderService
    {
        ObservableCollection<AudioProjectTreeNode> BuildTree(AudioProject audioProject, bool showEditedAudioProjectItemsOnly);
    }

    public class AudioProjectTreeBuilderService() : IAudioProjectTreeBuilderService
    {
        public ObservableCollection<AudioProjectTreeNode> BuildTree(AudioProject audioProject, bool showEditedAudioProjectItemsOnly)
        {
            var audioProjectTree = new ObservableCollection<AudioProjectTreeNode>();

            var actionEventsContainer = AudioProjectTreeNode.CreateContainerNode(AudioProjectTreeInfo.ActionEventSoundBanksContainerName, AudioProjectTreeNodeType.ActionEventSoundBanksContainer);
            var dialogueEventsContainer = AudioProjectTreeNode.CreateContainerNode(AudioProjectTreeInfo.DialogueEventSoundBanksContainer, AudioProjectTreeNodeType.DialogueEventSoundBanksContainer);
            var stateGroupsContainer = AudioProjectTreeNode.CreateContainerNode(AudioProjectTreeInfo.StateGroupsContainerName, AudioProjectTreeNodeType.StateGroupsContainer);

            var actionEventSoundBanks = showEditedAudioProjectItemsOnly
                ? audioProject.GetEditedActionEventSoundBanks()
                : audioProject.GetActionEventSoundBanks();

            foreach (var actionEventSoundBank in actionEventSoundBanks)
            {
                var node = AudioProjectTreeNode.CreateChildNode(actionEventSoundBank.Name, AudioProjectTreeNodeType.ActionEventSoundBank, actionEventsContainer);
                actionEventsContainer.Children.Add(node);
            }

            var dialogueEventSoundBanks = showEditedAudioProjectItemsOnly
                ? audioProject.GetEditedDialogueEventSoundBanks()
                : audioProject.GetDialogueEventSoundBanks();

            foreach (var dialogueEventSoundBank in dialogueEventSoundBanks)
            {
                var soundBankNode = AudioProjectTreeNode.CreateContainerNode(dialogueEventSoundBank.Name, AudioProjectTreeNodeType.DialogueEventSoundBank, dialogueEventsContainer);

                var dialogueEvents = showEditedAudioProjectItemsOnly
                    ? dialogueEventSoundBank.GetEditedDialogueEvents()
                    : dialogueEventSoundBank.DialogueEvents;

                foreach (var dialogueEvent in dialogueEvents)
                {
                    var node = AudioProjectTreeNode.CreateChildNode(dialogueEvent.Name, AudioProjectTreeNodeType.DialogueEvent, soundBankNode);
                    soundBankNode.Children.Add(node);
                }
                dialogueEventsContainer.Children.Add(soundBankNode);
            }

            var stateGroups = showEditedAudioProjectItemsOnly
                ? audioProject.GetEditedStateGroups()
                : audioProject.StateGroups;

            foreach (var stateGroup in stateGroups)
            {
                var node = AudioProjectTreeNode.CreateChildNode(stateGroup.Name, AudioProjectTreeNodeType.StateGroup, stateGroupsContainer);
                stateGroupsContainer.Children.Add(node);
            }

            audioProjectTree.Add(actionEventsContainer);
            audioProjectTree.Add(dialogueEventsContainer);
            audioProjectTree.Add(stateGroupsContainer);

            return audioProjectTree;
        }
    }
}
