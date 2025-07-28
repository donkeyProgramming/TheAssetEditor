using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    // TODO: Change these to DialogueEventSoundBanksContainer and ActionEventSoundBanksContainer
    public enum AudioProjectExplorerTreeNodeType
    {
        ActionEventsContainer,
        ActionEventSoundBank,
        DialogueEventsContainer,
        DialogueEventSoundBank,
        DialogueEvent,
        StateGroupsContainer,
        StateGroup
    }

    public partial class AudioProjectExplorerTreeNode : ObservableObject
    {
        public string Name { get; set; }
        [ObservableProperty] public AudioProjectExplorerTreeNodeType _nodeType;
        [ObservableProperty] public AudioProjectExplorerTreeNode _parent;
        [ObservableProperty] public ObservableCollection<AudioProjectExplorerTreeNode> _children = [];
        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] public string _presetFilterDisplayText;
        [ObservableProperty] public DialogueEventPreset? _presetFilter = DialogueEventPreset.ShowAll;

        public static AudioProjectExplorerTreeNode CreateContainer(string name, AudioProjectExplorerTreeNodeType nodeType, AudioProjectExplorerTreeNode parent = null)
        {
            return new AudioProjectExplorerTreeNode
            {
                Name = name,
                NodeType = nodeType,
                Parent = parent,
                Children = []
            };
        }

        public static AudioProjectExplorerTreeNode CreateChildNode(string name, AudioProjectExplorerTreeNodeType nodeType, AudioProjectExplorerTreeNode parent)
        {
            return new AudioProjectExplorerTreeNode
            {
                Name = name,
                NodeType = nodeType,
                Parent = parent
            };
        }

        public static AudioProjectExplorerTreeNode GetNode(ObservableCollection<AudioProjectExplorerTreeNode> audioProjectTree, string nodeName)
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
            if (NodeType == AudioProjectExplorerTreeNodeType.ActionEventSoundBank)
                return true;
            return false;
        }

        public bool IsDialogueEventSoundBank()
        {
            if (NodeType == AudioProjectExplorerTreeNodeType.DialogueEventSoundBank)
                return true;
            return false;
        }

        public bool IsDialogueEvent()
        {
            if (NodeType == AudioProjectExplorerTreeNodeType.DialogueEvent)
                return true;
            return false;
        }

        public bool IsStateGroup()
        {
            if (NodeType == AudioProjectExplorerTreeNodeType.StateGroup)
                return true;
            return false;
        }
    }
}
