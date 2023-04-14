using Audio.FileFormats.WWise;
using System.Collections.Generic;

namespace Audio.AudioEditor
{
    public class HircTreeItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public HircItem Item { get; set; }


        public List<HircTreeItem> Children { get; set; } = new List<HircTreeItem>();
        public HircTreeItem Parent { get; set; } = null;
    }
}
