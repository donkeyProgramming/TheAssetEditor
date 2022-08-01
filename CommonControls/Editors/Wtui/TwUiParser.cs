using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CommonControls.Editors.Wtui
{
    class TwUiParser
    {
        public static UiTreeNode GenerateLayoutTree(string xmlText, Dictionary<string, string> componentList)
        {
            var document = new XmlDocument();
            document.LoadXml(xmlText);

            var rootNode = new UiTreeNode();
            var layoutNode = document.SelectNodes("layout/hierarchy/root");
            foreach (XmlNode xmlNode in layoutNode)
                GenerateLayoutTree(xmlNode, rootNode, componentList);

            return rootNode;
        }

        static void GenerateLayoutTree(XmlNode xmlNode, UiTreeNode treeNode, Dictionary<string, string> componentList)
        {
            treeNode.Guid.Value = xmlNode.Attributes.GetNamedItem("this").InnerText;
            treeNode.Name.Value = xmlNode.Name;
            if (componentList.TryGetValue(treeNode.Guid.Value, out var componentXml))
                treeNode.XmlContent = componentXml;

            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                var childNode = new UiTreeNode();
                treeNode.Children.Add(childNode);
                GenerateLayoutTree(child, childNode, componentList);
            }
        }

        public static Dictionary<string, string> LoadAllComponents(string xmlText)
        {
            var output = new Dictionary<string, string>();
            var document = new XmlDocument();
            document.LoadXml(xmlText);

            var componentNode = document.SelectSingleNode("layout/components");
            foreach (XmlNode node in componentNode.ChildNodes)
            {
                var guid = node.Attributes.GetNamedItem("this").InnerText;
                var niceXmlText = PrettyXml(node.OuterXml);

                output.Add(guid, niceXmlText);
            }

            return output;
        }


        static string PrettyXml(string xml)
        {
            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml);

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }

    }
}
