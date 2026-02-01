using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using static Editors.Audio.AudioEditor.Presentation.Shared.Models.AudioProjectTreeNodeType;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Models
{
    public enum AudioProjectTreeNodeType
    {
        // The container node for SoundBanks
        SoundBanks,
        // The SoundBank node
        SoundBank,

        // The container node for ActionEvents
        ActionEvents,
        // The Action Event Type e.g. Movies or Music
        ActionEventType,

        // The container node for Dialogue Events
        DialogueEvents,
        // The Dialogue Event node
        DialogueEvent,

        // The container node for State Groups
        StateGroups,
        // The State Group node
        StateGroup
    }

    public partial class AudioProjectTreeNode : ObservableObject
    {
        public string Name { get; set; }
        public AudioProjectTreeNodeType Type { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }
        public AudioProjectTreeNode Parent { get; set; }
        public ObservableCollection<AudioProjectTreeNode> Children { get; set; } = [];
        [ObservableProperty] bool _isExpanded = false;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] public string _dialogueEventFilterDisplayText;
        [ObservableProperty] public Wh3DialogueEventType? _dialogueEventTypeFilter = Wh3DialogueEventType.TypeShowAll;
        [ObservableProperty] public Wh3DialogueEventUnitProfile? _dialogueEventProfileFilter = Wh3DialogueEventUnitProfile.ProfileShowAll;

        public static AudioProjectTreeNode CreateNode(string name, AudioProjectTreeNodeType nodeType, Wh3SoundBank gameSoundBank = Wh3SoundBank.None, AudioProjectTreeNode parent = null)
        {
            return new AudioProjectTreeNode
            {
                Name = name,
                Type = nodeType,
                GameSoundBank = gameSoundBank,
                Parent = parent
            };
        }

        public bool IsActionEvent()
        {
            if (Type == ActionEventType)
                return true;
            return false;
        }

        public bool IsDialogueEvents()
        {
            if (Name == AudioProjectTreeBuilderService.DialogueEventsNodeName)
                return true;
            return false;
        }

        public bool IsDialogueEvent()
        {
            if (Type == DialogueEvent)
                return true;
            return false;
        }

        public bool IsStateGroup()
        {
            if (Type == StateGroup)
                return true;
            return false;
        }

        public bool IsSoundBank()
        {
            if (Type == SoundBank)
                return true;
            return false;
        }

        public bool IsMusicActionEvent()
        {
            if (IsActionEvent() && Name == Wh3ActionEventInformation.GetName(Wh3ActionEventType.Music))
                return true;
            return false;
        }

        public bool IsBattleAbilityActionEvent()
        {
            if (IsActionEvent() && Name == Wh3ActionEventInformation.GetName(Wh3ActionEventType.BattleAbilities))
                return true;
            return false;
        }

        public bool IsMovieActionEvent()
        {
            if (IsActionEvent() && Name == Wh3ActionEventInformation.GetName(Wh3ActionEventType.Movies))
                return true;
            return false;
        }

        public AudioProjectTreeNode GetParentSoundBankNode()
        {
            var currentNode = Parent;

            while (currentNode != null)
            {
                if (currentNode.IsSoundBank())
                    return currentNode;

                currentNode = currentNode.Parent;
            }

            return null;
        }
    }
}
