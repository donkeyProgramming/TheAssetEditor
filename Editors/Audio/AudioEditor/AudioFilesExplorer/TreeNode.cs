using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public enum NodeType
    {
        Directory,
        WavFile
    }

    public partial class TreeNode : ObservableObject
    {
        public string Name { get; set; }
        public NodeType NodeType { get; set; }
        public TreeNode Parent { get; set; }
        public string FilePath { get; set; }

        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] ObservableCollection<TreeNode> _children = [];
    }
}
