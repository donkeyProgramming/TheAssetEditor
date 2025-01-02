using System.Xml.Linq;
using Shared.GameFormats.Twui.Data;

namespace Shared.GameFormats.Twui
{
    public class HierarchySerializer
    {
        public static Hierarchy Serialize(XElement? hierarchyNode)
        {
            if (hierarchyNode == null)
                return new();

            var nodes = Process(hierarchyNode.FirstNode as XElement, null);
            return new Hierarchy()
            {
                RootItems = nodes,
            };
        }

        private static List<HierarchyItem> Process(XElement hierarchyNode, HierarchyItem? parentHierarchyItem)
        {
            var output = new List<HierarchyItem>();

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
}
