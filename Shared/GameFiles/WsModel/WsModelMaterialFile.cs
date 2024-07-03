using System.Text;
using System.Xml;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.WsModel
{
    public class WsModelMaterialFile
    {
        public bool Alpha { get; set; } = false;
        public Dictionary<TextureType, string> Textures { get; set; } = new Dictionary<TextureType, string>();
        public UiVertexFormat VertexType { get; set; } = UiVertexFormat.Unknown;
        public string Name { get; set; }
        public string FullPath { get; set; }

        public WsModelMaterialFile(PackFile pf, string fullPath)
        {
            FullPath = fullPath;

            var buffer = pf.DataSource.ReadData();
            var xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            var doc = new XmlDocument();
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
                else if (Name.Contains("weighted_standard_4", StringComparison.InvariantCultureIgnoreCase))
                    VertexType = UiVertexFormat.Cinematic;
                else if (Name.Contains("weighted_standard_2", StringComparison.InvariantCultureIgnoreCase))
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

            if (textureSlotName.Contains("diffuse"))
                    Textures[TextureType.Diffuse] = texturePath;
            if (textureSlotName.Contains("gloss"))
                    Textures[TextureType.Gloss] = texturePath;
            if (textureSlotName.Contains("mask"))
                    Textures[TextureType.Mask] = texturePath;
            if (textureSlotName.Contains("normal"))
                    Textures[TextureType.Normal] = texturePath;
            if (textureSlotName.Contains("specular"))
                    Textures[TextureType.Specular] = texturePath;
            if (textureSlotName.Contains("base_colour"))
                    Textures[TextureType.BaseColour] = texturePath;
            if (textureSlotName.Contains("material_map"))
                    Textures[TextureType.MaterialMap] = texturePath;
            }
        }
    }
}
