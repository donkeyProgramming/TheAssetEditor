using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.VfxEditor.ViewModels
{
    public enum NodeKind { Effect, Emitters, Emitter, Chain, Modifier, Stack, References, Reference, Override, ShaderLibrary, EditorData, Component }

    /// <summary>A node of the effect tree. Key names it across rebuilds, so selection and expansion survive an edit; Path
    /// finds its element in the current document.</summary>
    public sealed partial class VfxTreeNode(NodeKind kind, string label, string key, int[] path) : ObservableObject
    {
        public NodeKind Kind { get; } = kind;
        [ObservableProperty] string _label = label;
        public string Key { get; } = key;
        public int[] Path { get; set; } = path;
        public string? Fragment { get; init; }
        public int Occurrence { get; init; }
        public ObservableCollection<VfxTreeNode> Children { get; } = [];

        [ObservableProperty] bool _isExpanded;
        [ObservableProperty] bool _isSelected;
    }
}
