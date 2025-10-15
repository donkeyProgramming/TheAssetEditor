using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer
{
    // TODO: Add game abstraction
    public interface IAudioProjectTreeBuilderService
    {
        ObservableCollection<AudioProjectTreeNode> BuildTree(AudioProjectFile audioProject, bool showEditedItemsOnly);
    }

    public class AudioProjectTreeBuilderService(IAudioEditorStateService audioEditorStateService) : IAudioProjectTreeBuilderService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        public const string SoundBanksNodeName = "SoundBanks";
        public const string ActionEventsNodeName = "Action Events";
        public const string DialogueEventsNodeName = "Dialogue Events";
        public const string StateGroupsContainerName = "State Groups";

        public ObservableCollection<AudioProjectTreeNode> BuildTree(AudioProjectFile audioProject, bool showEditedItemsOnly)
        {
            var audioProjectTree = new ObservableCollection<AudioProjectTreeNode>();

            var soundBanksNode = AudioProjectTreeNode.CreateNode(SoundBanksNodeName, AudioProjectTreeNodeType.SoundBanks);
            var stateGroupsNode = AudioProjectTreeNode.CreateNode(StateGroupsContainerName, AudioProjectTreeNodeType.StateGroups);

            var soundBanks = showEditedItemsOnly
                ? audioProject.GetEditedSoundBanks()
                : audioProject.SoundBanks;

            foreach (var soundBank in soundBanks)
            {
                var audioProjectFileName = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
                var soundBankName = $"{Wh3SoundBankInformation.GetName(soundBank.GameSoundBank)}";
                var soundBankDisplayName = $"{soundBankName}_{audioProjectFileName}";
                var soundBankNode = AudioProjectTreeNode.CreateNode(soundBankDisplayName, AudioProjectTreeNodeType.SoundBank, gameSoundBank: soundBank.GameSoundBank, parent: soundBanksNode);

                if (Wh3ActionEventInformation.Contains(soundBank.GameSoundBank))
                {
                    var actionEventsNode = AudioProjectTreeNode.CreateNode(ActionEventsNodeName, AudioProjectTreeNodeType.ActionEvents, gameSoundBank: soundBank.GameSoundBank, parent: soundBankNode);
                    soundBankNode.Children.Add(actionEventsNode);

                    var actionEventTypes = showEditedItemsOnly
                        ? soundBank.GetUsedActionEventTypes()
                        : Wh3ActionEventInformation.GetSoundBankActionEventTypes(soundBank.GameSoundBank);

                    foreach (var actionEventType in actionEventTypes)
                    {
                        var actionEventGroupName = Wh3ActionEventInformation.GetName(actionEventType);
                        var actionEventTypeNode = AudioProjectTreeNode.CreateNode(actionEventGroupName, AudioProjectTreeNodeType.ActionEventType, gameSoundBank: soundBank.GameSoundBank, parent: actionEventsNode);
                        actionEventsNode.Children.Add(actionEventTypeNode);
                    }
                }

                if (Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank))
                {
                    var dialogueEventsNode = AudioProjectTreeNode.CreateNode(DialogueEventsNodeName, AudioProjectTreeNodeType.DialogueEvents, gameSoundBank: soundBank.GameSoundBank, parent: soundBankNode);
                    soundBankNode.Children.Add(dialogueEventsNode);

                    var dialogueEvents = showEditedItemsOnly
                        ? soundBank.GetEditedDialogueEvents()
                        : soundBank.DialogueEvents;

                    var sortedDialogueEvents = dialogueEvents.OrderBy(dialogueEvent => dialogueEvent.Name, StringComparer.OrdinalIgnoreCase);
                    foreach (var dialogueEvent in sortedDialogueEvents)
                    {
                        var dialogueEventNode = AudioProjectTreeNode.CreateNode(dialogueEvent.Name, AudioProjectTreeNodeType.DialogueEvent, gameSoundBank: soundBank.GameSoundBank, parent: dialogueEventsNode);
                        dialogueEventsNode.Children.Add(dialogueEventNode);
                    }
                }

                soundBanksNode.Children.Add(soundBankNode);
            }

            var stateGroups = showEditedItemsOnly
                ? audioProject.GetEditedStateGroups()
                : audioProject.StateGroups;

            foreach (var stateGroup in stateGroups)
            {
                var stateGroupNode = AudioProjectTreeNode.CreateNode(stateGroup.Name, AudioProjectTreeNodeType.StateGroup, parent: stateGroupsNode);
                stateGroupsNode.Children.Add(stateGroupNode);
            }

            audioProjectTree.Add(soundBanksNode);
            audioProjectTree.Add(stateGroupsNode);
            return audioProjectTree;
        }
    }
}
