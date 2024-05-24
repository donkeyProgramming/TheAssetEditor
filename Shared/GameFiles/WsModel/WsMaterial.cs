using System.Text;
using System.Xml;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.WsModel
{
    public class WsMaterial
    {
        public class WsMaterialEntry
        {
            public int PartIndex { get; set; }
            public int LodIndex { get; set; }
            public string Material { get; set; }
        }
        public string GeometryPath { get; set; } = "";
        public List<WsMaterialEntry> MaterialList { get; set; } = new List<WsMaterialEntry>();

        public WsMaterial(PackFile file)
        {
            var buffer = file.DataSource.ReadData();
            var xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var doc = new XmlDocument();
            doc.LoadXml(xmlString);

            var geometryNodes = doc.SelectNodes(@"/model/geometry");
            if (geometryNodes.Count != 0)
                GeometryPath = geometryNodes.Item(0).InnerText;

            var materialNodes = doc.SelectNodes(@"/model/materials/material");
            foreach (XmlNode materialNode in materialNodes)
            {
                MaterialList.Add(new WsMaterialEntry()
                {
                    LodIndex = int.Parse(materialNode.Attributes.GetNamedItem("lod_index").InnerText),
                    PartIndex = int.Parse(materialNode.Attributes.GetNamedItem("part_index").InnerText),
                    Material = materialNode.InnerText
                });
            }
        }
    }
}
