using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.Presentation.Shared
{
    public enum AudioFilesTreeNodeType
    {
        Directory,
        WavFile
    }

    public partial class AudioFilesTreeNode : ObservableObject
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public AudioFilesTreeNodeType Type { get; set; }
        public AudioFilesTreeNode Parent { get; set; }
        public ObservableCollection<AudioFilesTreeNode> Children { get; set; } = [];
        [ObservableProperty] bool _isExpanded = false;
        [ObservableProperty] bool _isVisible = true;

        public event EventHandler<bool> NodeIsExpandedChanged;

        public static AudioFilesTreeNode CreateContainerNode(string name, AudioFilesTreeNodeType nodeType, AudioFilesTreeNode parent = null)
        {
            return new AudioFilesTreeNode
            {
                FileName = name,
                Type = nodeType,
                Parent = parent,
            };
        }

        public static AudioFilesTreeNode CreateChildNode(string name, AudioFilesTreeNodeType nodeType, AudioFilesTreeNode parent)
        {
            return new AudioFilesTreeNode
            {
                FileName = name,
                Type = nodeType,
                Parent = parent
            };
        }

        partial void OnIsExpandedChanged(bool value) => NodeIsExpandedChanged?.Invoke(this, value);
    }
}
