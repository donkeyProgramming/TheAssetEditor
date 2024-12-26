using Shared.GameFormats.Wwise;
using System.Collections.Generic;

namespace Editors.Audio.AudioExplorer
{
    public class HircTreeItem
    {
        public bool IsExpanded { get; set; } = true;

        public string DisplayName { get; set; } = string.Empty;
        public HircItem Item { get; set; }
        public bool IsMetaNode { get; set; } // things like switch nodes


        public List<HircTreeItem> Children { get; set; } = new List<HircTreeItem>();
        public HircTreeItem Parent { get; set; } = null;
    }
}
