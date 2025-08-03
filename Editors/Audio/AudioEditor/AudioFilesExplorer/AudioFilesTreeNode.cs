using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public enum AudioFilesTreeNodeType
    {
        Directory,
        WavFile
    }

    public partial class AudioFilesTreeNode : ObservableObject
    {
        public string Name { get; set; }
        public AudioFilesTreeNodeType NodeType { get; set; }
        public AudioFilesTreeNode Parent { get; set; }
        public ObservableCollection<AudioFilesTreeNode> Children { get; set; } = [];
        public string FilePath { get; set; }
        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] bool _isVisible = true;

        public static AudioFilesTreeNode CreateContainerNode(string name, AudioFilesTreeNodeType nodeType, AudioFilesTreeNode parent = null)
        {
            return new AudioFilesTreeNode
            {
                Name = name,
                NodeType = nodeType,
                Parent = parent,
            };
        }

        public static AudioFilesTreeNode CreateChildNode(string name, AudioFilesTreeNodeType nodeType, AudioFilesTreeNode parent)
        {
            return new AudioFilesTreeNode
            {
                Name = name,
                NodeType = nodeType,
                Parent = parent
            };
        }
    }
}
