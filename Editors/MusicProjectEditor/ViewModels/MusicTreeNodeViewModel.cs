using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>One node of an event's music tree, as the tree editor shows it.</summary>
    public partial class MusicTreeNodeViewModel : ObservableObject
    {
        public MusicTreeNode Node { get; }
        public MusicTreeNodeViewModel? Parent { get; }
        public ObservableCollection<MusicTreeNodeViewModel> Children { get; } = [];

        [ObservableProperty] bool _isExpanded = true;
        [ObservableProperty] bool _isSelected;

        public bool IsRoot => Parent == null;

        public string Label => IsRoot
            ? "All of this culture's music"
            : $"{Wh3MusicTreeLevelInformation.GetLevelName(Parent!.Node.SplitBy)}: {Wh3MusicTreeLevelInformation.GetValueName(Parent.Node.SplitBy, Node.Value)}";

        public string Summary => Node.AudioFileIds.Count switch
        {
            0 when IsRoot => "no audio yet",
            0 => "plays its parent's",
            1 => "1 wav",
            var count => $"{count} wavs"
        };

        public MusicTreeNodeViewModel(MusicTreeNode node, MusicTreeNodeViewModel? parent)
        {
            Node = node;
            Parent = parent;
            foreach (var branch in node.Branches)
                Children.Add(new MusicTreeNodeViewModel(branch, this));
        }

        public IEnumerable<MusicTreeNodeViewModel> SelfAndDescendants() => new[] { this }.Concat(Children.SelectMany(child => child.SelfAndDescendants()));

        /// <summary>The levels this node and the branches above it split by, which it cannot split by
        /// again.</summary>
        public IEnumerable<string> LevelsSplitAbove()
        {
            for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
                yield return ancestor.Node.SplitBy;
        }

        /// <summary>The containers this node can play in, given the part-of-the-game splits above it.</summary>
        public bool IsLimitedToContainerOtherThan(uint containerId)
        {
            for (var node = this; node.Parent != null; node = node.Parent)
            {
                if (node.Parent.Node.SplitBy == Wh3MusicTreeLevelInformation.ContainerLevel && node.Node.Value != null && node.Node.Value != containerId.ToString())
                    return true;
            }
            return false;
        }
    }
}
