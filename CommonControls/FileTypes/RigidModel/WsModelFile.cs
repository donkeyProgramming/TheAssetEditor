using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel.Types;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace CommonControls.FileTypes.RigidModel
{
    public class WsModelFile
    {
        public bool Alpha { get; set; } = false;
        public Dictionary<TexureType, string> Textures { get; set; } = new Dictionary<TexureType, string>();
        public UiVertexFormat VertexType { get; set; } = UiVertexFormat.Unknown;
        public string Name { get; set; }
        public string FullPath { get; set; }

        public WsModelFile(PackFile pf, string fullPath)
        {
            FullPath = fullPath;

            var buffer = pf.DataSource.ReadData();
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            var nameNode = doc.SelectSingleNode(@"/material/name");
            if (nameNode != null)
            {
                Name = nameNode.InnerText;
                if (Name.Contains("alpha_on", StringComparison.InvariantCultureIgnoreCase))
                    Alpha = true;

                if (Name.Contains("weighted4", StringComparison.InvariantCultureIgnoreCase))
                    VertexType = UiVertexFormat.Cinematic;
                else if (Name.Contains("weighted2", StringComparison.InvariantCultureIgnoreCase))
                    VertexType = UiVertexFormat.Weighted;
                else
                    VertexType = UiVertexFormat.Static;
            }

            var textureNodes = doc.SelectNodes(@"/material/textures/texture");
            foreach (XmlNode node in textureNodes)
            {
                var slotNode = node.SelectNodes("slot");
                var pathNode = node.SelectNodes("source");

                if (pathNode.Count == 0 || slotNode.Count == 0)
                    continue;

                var textureSlotName = slotNode[0].InnerText;
                var texturePath = pathNode[0].InnerText;

                if (textureSlotName == "s_diffuse")
                    Textures[TexureType.Diffuse] = texturePath;

                if (textureSlotName == "s_gloss")
                    Textures[TexureType.Gloss] = texturePath;

                if (textureSlotName == "s_mask")
                    Textures[TexureType.Mask] = texturePath;
                else if (textureSlotName == "s_mask1")
                    Textures[TexureType.Mask] = texturePath;

                if (textureSlotName == "s_normal")
                    Textures[TexureType.Normal] = texturePath;

                if (textureSlotName == "s_specular")
                    Textures[TexureType.Specular] = texturePath;
            }
        }
    }
}
