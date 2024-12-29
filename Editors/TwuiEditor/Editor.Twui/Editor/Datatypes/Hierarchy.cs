using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Twui.Editor.Datatypes
{
    public partial class Hierarchy : ObservableObject
    {
        [ObservableProperty] ObservableCollection<HierarchyItem> _rootItems = [];

        public static Hierarchy Serialize(XElement hierarchyNode)
        {
            var nodes = Process(hierarchyNode.FirstNode as XElement, null);
            return new Hierarchy()
            {
                RootItems = nodes,
            };
        }

        private static ObservableCollection<HierarchyItem> Process(XElement hierarchyNode, HierarchyItem? parentHierarchyItem)
        {
            var output = new ObservableCollection<HierarchyItem>();

            var currentXmlNode = hierarchyNode;
            if (currentXmlNode != null)
            {
                var idAttribute = currentXmlNode.Attribute("this");
                var name = currentXmlNode.Name.LocalName;
                var hierarchyItem = new HierarchyItem()
                {
                    Name = name,
                    Id = idAttribute.Value
                };

                if (parentHierarchyItem != null)
                    parentHierarchyItem.Children.Add(hierarchyItem);
                else
                    output.Add(hierarchyItem);

                foreach (var child in currentXmlNode.Elements())
                    Process(child, hierarchyItem);
            }

            return output;

        }
    }

    [DebuggerDisplay("{Name} - {Id}")]
    public class HierarchyItem
    {
        public List<HierarchyItem> Children { get; set; } = new List<HierarchyItem>();

        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }
}
