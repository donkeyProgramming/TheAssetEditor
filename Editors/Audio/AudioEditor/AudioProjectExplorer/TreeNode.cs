using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public enum NodeType
    {
        ActionEventsContainer,
        ActionEventSoundBank,
        DialogueEventsContainer,
        DialogueEventSoundBank,
        DialogueEvent,
        StateGroupsContainer,
        StateGroup
    }

    public partial class TreeNode : ObservableObject
    {
        public string Name { get; set; }
        [ObservableProperty] public NodeType _nodeType;
        [ObservableProperty] public TreeNode _parent;
        [ObservableProperty] public ObservableCollection<TreeNode> _children = [];
        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] bool _isVisible = true;
        [ObservableProperty] public string _presetFilterDisplayText;
        [ObservableProperty] public DialogueEventPreset? _presetFilter = DialogueEventPreset.ShowAll;

        public static TreeNode GetAudioProjectTreeNodeFromName(ObservableCollection<TreeNode> audioProjectTree, string nodeName)
        {
            foreach (var node in audioProjectTree)
            {
                if (node.Name == nodeName)
                    return node;

                var childNode = GetAudioProjectTreeNodeFromName(node.Children, nodeName);
                if (childNode != null)
                    return childNode;
            }

            return null;
        }
    }
}
