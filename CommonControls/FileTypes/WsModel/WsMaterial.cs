// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Xml;
using CommonControls.FileTypes.PackFiles.Models;

namespace CommonControls.FileTypes.WsModel
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
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            XmlDocument doc = new XmlDocument();
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
