using System.Collections.Generic;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioExplorer
{
    public class HircTreeNode
    {
        public bool IsExpanded { get; set; } = true;
        public string DisplayName { get; set; } = string.Empty;
        public HircItem Item { get; set; }
        public bool IsMetaNode { get; set; } // things like switch nodes
        public List<HircTreeNode> Children { get; set; } = [];
        public HircTreeNode Parent { get; set; } = null;
    }
}
