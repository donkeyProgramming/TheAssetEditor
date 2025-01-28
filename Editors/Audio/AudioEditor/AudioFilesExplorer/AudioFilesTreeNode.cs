using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public enum NodeType
    {
        Directory,
        WavFile
    }

    public partial class AudioFilesTreeNode : ObservableObject
    {
        public string Name { get; set; }
        public NodeType NodeType { get; set; }
        public AudioFilesTreeNode Parent { get; set; }
        public string FilePath { get; set; }

        [ObservableProperty] bool _isNodeExpanded = false;
        [ObservableProperty] ObservableCollection<AudioFilesTreeNode> _children = [];
    }
}
