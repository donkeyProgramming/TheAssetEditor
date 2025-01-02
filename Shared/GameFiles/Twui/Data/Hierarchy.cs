using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Shared.GameFormats.Twui.Data
{
    public class Hierarchy
    {
        public List<HierarchyItem> RootItems { get; set; } = [];
    }

    [DebuggerDisplay("{Name} - {Id}")]
    public class HierarchyItem
    {
        public List<HierarchyItem> Children { get; set; } = [];

        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }
}
