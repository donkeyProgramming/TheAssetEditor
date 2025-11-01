using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using static Editors.Audio.AudioEditor.Presentation.Shared.AudioProjectTreeNodeType;

namespace Editors.Audio.AudioEditor.Presentation.Shared
{
    public enum AudioProjectTreeNodeType
    {
        SoundBanks,
        SoundBank,
        ActionEvents,
        DialogueEvents,
        ActionEventType,
        DialogueEvent,
        StateGroups,
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

        public static AudioProjectTreeNode GetNode(ObservableCollection<AudioProjectTreeNode> audioProjectTree, string nodeName)
        {
            foreach (var node in audioProjectTree)
            {
                if (node.Name == nodeName)
                    return node;

                var childNode = GetNode(node.Children, nodeName);
                if (childNode != null)
                    return childNode;
            }

            return null;
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
    }
}
