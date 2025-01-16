using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Shared.GameFormats.Twui.Data
{
    public class Hierarchy
    {
        public List<HierarchyItem> RootItems { get; set; } = [];
    }

    [DebuggerDisplay("{Name} - {Id}")]
    public partial class HierarchyItem : ObservableObject
    {
        public List<HierarchyItem> Children { get; set; } = [];

        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;


        public string Priority { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsVisible { get; set; } = true;

    }
}
