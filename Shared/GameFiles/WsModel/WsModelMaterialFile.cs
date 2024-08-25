using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.WsModel
{

    public class WsModelMaterialParam
    { 
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public WsModelMaterialParam() { }
        public WsModelMaterialParam(string name, string value) { Name = name; Type = "float"; Value = value; }
        public WsModelMaterialParam(string name, float value) { Name = name; Type = "float"; Value = value.ToString(); }
        public WsModelMaterialParam(string name, Vector2 value) { Name = name; Type = "float2"; Value = $"{value.X},{value.Y}"; }
        public WsModelMaterialParam(string name, Vector3 value) { Name = name; Type = "float3"; Value = $"{value.X},{value.Y},{value.Z}"; }
    }

    public class WsModelMaterialFile
    {
        public bool Alpha { get; set; } = false;
        public Dictionary<TextureType, string> Textures { get; set; } = [];
        public UiVertexFormat VertexType { get; set; } = UiVertexFormat.Unknown;
        public string Name { get; set; } = string.Empty;

        public List<WsModelMaterialParam> Parameters { get; set; } = [];
        public string ShaderPath { get; set; } = string.Empty;

        public WsModelMaterialFile(PackFile pf)
        {
            var buffer = pf.DataSource.ReadData();
            var xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            LoadContent(xmlString);
        }

        public WsModelMaterialFile(string fileContent) => LoadContent(fileContent);

        public WsModelMaterialFile() { }

        public WsModelMaterialParam GetParameter(string name) => Parameters.First(p => p.Name == name);
        public WsModelMaterialParam GetParameter(WsModelParamters.Instance instance) => Parameters.First(p => p.Name == instance.Name);

        void LoadContent(string fileContent)
        {
            fileContent = RemoveBOM(fileContent);

            var doc = new XmlDocument();
            doc.LoadXml(fileContent);

            ExtractParameters(doc);
            ExtractShaderName(doc);
            ExtractInformationFromName(doc);
            ExtractTextures(doc);
        }

        private string RemoveBOM(string xml)
        {
            var index = xml.IndexOf('<');
            if (index > 0)
            {
                xml = xml.Substring(index, xml.Length - index);
            }

            return xml;
        }

        private void ExtractShaderName(XmlDocument doc)
        {
            var node = doc.SelectSingleNode(@"/material/shader");
            if (node == null)
                return;
            ShaderPath = node.InnerText;
        }

        void ExtractParameters(XmlDocument doc)
        {
            var parameterNodes = doc.SelectNodes(@"/material/params/param");
            if (parameterNodes == null)
                return;

            foreach (XmlNode paramNode in parameterNodes)
            {
                var paramName = paramNode.SelectSingleNode("name")!.InnerText;
                var paramType = paramNode.SelectSingleNode("type")!.InnerText;
                var paramValue = paramNode.SelectSingleNode("value")!.InnerText;

                Parameters.Add(new WsModelMaterialParam() { Name = paramName, Type = paramType, Value = paramValue });
            }
        }

        void ExtractInformationFromName(XmlDocument doc)
        {
            var nameNode = doc.SelectSingleNode(@"/material/name");
            if (nameNode == null)
                return;
          
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

        void ExtractTextures(XmlDocument doc)
        {
            var textureNodes = doc.SelectNodes(@"/material/textures/texture");
            if (textureNodes == null)
                return;
            foreach (XmlNode node in textureNodes)
            {
                var slotNode = node.SelectSingleNode("slot");
                var pathNode = node.SelectSingleNode("source");

                string? texturePath;
                if (pathNode == null)
                    texturePath = node.InnerText;
                else
                    texturePath = pathNode.InnerText;

                var textureSlotName = slotNode.InnerText;

                if (textureSlotName.Contains("diffuse", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Diffuse] = texturePath;
                if (textureSlotName.Contains("gloss", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Gloss] = texturePath;
                if (textureSlotName.Contains("mask", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Mask] = texturePath;
                if (textureSlotName.Contains("normal", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Normal] = texturePath;
                if (textureSlotName.Contains("specular", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Specular] = texturePath;
                if (textureSlotName.Contains("base_colour", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.BaseColour] = texturePath;
                if (textureSlotName.Contains("material_map", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.MaterialMap] = texturePath;
                if (textureSlotName.Contains("xml_blood_map", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Blood] = texturePath;
                if (textureSlotName.Contains("t_xml_emissive_distortion", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.EmissiveDistortion] = texturePath;
                if (textureSlotName.Contains("t_xml_emissive_texture", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Emissive] = texturePath;
                if (textureSlotName.Contains("t_xml_distortion", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.Distortion] = texturePath;
                if (textureSlotName.Contains("t_xml_distortion_noise", StringComparison.InvariantCultureIgnoreCase))
                    Textures[TextureType.DistortionNoise] = texturePath;
            }
        }
    }
}
