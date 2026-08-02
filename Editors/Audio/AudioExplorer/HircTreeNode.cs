using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioExplorer
{
    public partial class HircTreeNode : ObservableObject
    {
        [ObservableProperty] private bool _isExpanded = false;

        public string DisplayName { get; set; } = string.Empty;
        public HircItem Hirc { get; set; }
        public bool IsMetaNode { get; set; } // things like switch nodes
        public ObservableCollection<HircTreeNode> Children { get; set; } = [];
        public HircTreeNode Parent { get; set; } = null;

        internal List<uint> PendingChildHircIds { get; } = [];
        internal Action<HircTreeNode> ResolveChildrenCallback { get; set; }

        partial void OnIsExpandedChanged(bool value)
        {
            if (!value || ResolveChildrenCallback == null)
                return;

            var resolveChildren = ResolveChildrenCallback;
            ResolveChildrenCallback = null;
            resolveChildren(this);
        }
    }
}
