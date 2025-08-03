using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public enum AudioProjectTreeNodeType
    {
        ActionEventSoundBanksContainer,
        ActionEventSoundBank,
        DialogueEventSoundBanksContainer,
        DialogueEventSoundBank,
        DialogueEvent,
        StateGroupsContainer,
        StateGroup
    }

    public partial class AudioProjectTreeNode : ObservableObject
    {
        public string Name { get; set; }
        public AudioProjectTreeNodeType NodeType { get; set; }
        public AudioProjectTreeNode Parent { get; set; }
        public ObservableCollection<AudioProjectTreeNode> Children { get; set; } = [];
        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] public string _presetFilterDisplayText;
        [ObservableProperty] public DialogueEventPreset? _presetFilter = DialogueEventPreset.ShowAll;

        public static AudioProjectTreeNode CreateContainerNode(string name, AudioProjectTreeNodeType nodeType, AudioProjectTreeNode parent = null)
        {
            return new AudioProjectTreeNode
            {
                Name = name,
                NodeType = nodeType,
                Parent = parent
            };
        }

        public static AudioProjectTreeNode CreateChildNode(string name, AudioProjectTreeNodeType nodeType, AudioProjectTreeNode parent)
        {
            return new AudioProjectTreeNode
            {
                Name = name,
                NodeType = nodeType,
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

        public bool IsActionEventSoundBank()
        {
            if (NodeType == AudioProjectTreeNodeType.ActionEventSoundBank)
                return true;
            return false;
        }

        public bool IsDialogueEventSoundBank()
        {
            if (NodeType == AudioProjectTreeNodeType.DialogueEventSoundBank)
                return true;
            return false;
        }

        public bool IsDialogueEvent()
        {
            if (NodeType == AudioProjectTreeNodeType.DialogueEvent)
                return true;
            return false;
        }

        public bool IsStateGroup()
        {
            if (NodeType == AudioProjectTreeNodeType.StateGroup)
                return true;
            return false;
        }
    }
}
