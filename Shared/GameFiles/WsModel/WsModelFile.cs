using System.Text;
using System.Xml;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.WsModel
{
    public record WsModelFileEntry
    {
        public required int PartIndex { get; set; }
        public required int LodIndex { get; set; }
        public required string MaterialPath { get; set; }
    }

    public class WsModelFile
    {
        public string GeometryPath { get; set; } = "";
        public List<WsModelFileEntry> MaterialList { get; set; } = [];

        public WsModelFile(PackFile file)
        {
            var buffer = file.DataSource.ReadData();
            var xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var doc = new XmlDocument();
            doc.LoadXml(xmlString);

            var geometryNodes = doc.SelectNodes(@"/model/geometry");
            if (geometryNodes.Count != 0)
                GeometryPath = geometryNodes.Item(0).InnerText;

            var materialNodes = doc.SelectNodes(@"/model/materials/material");
            if (materialNodes == null)
                return;

            foreach (XmlNode materialNode in materialNodes)
            {

                var lodIndex = -1;
                var lodIndexNode = materialNode.Attributes.GetNamedItem("lod_index");
                if (lodIndexNode != null)
                    lodIndex = int.Parse(lodIndexNode.InnerText);


                var partIndex = -1;
                var partIndexNode = materialNode.Attributes.GetNamedItem("part_index");
                if (partIndexNode != null)
                    partIndex = int.Parse(partIndexNode.InnerText);

                MaterialList.Add(new WsModelFileEntry()
                {
                    LodIndex = lodIndex,
                    PartIndex = partIndex,
                    MaterialPath = materialNode.InnerText
                });
            }
        }
    }
}
