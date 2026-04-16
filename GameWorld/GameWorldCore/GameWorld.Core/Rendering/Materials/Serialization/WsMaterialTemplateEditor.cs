using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Shaders;
using Microsoft.Xna.Framework;
using Shared.Core.Settings;
using Shared.EmbeddedResources;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class WsMaterialTemplateEditor 
    {
        record GameEntry(GameTypeEnum Game, CapabilityMaterialsEnum MaterialType, string Path);

        static readonly List<GameEntry> s_materialToTemplateMap =
        [
            // Default - spec gloss games
            new (GameTypeEnum.Troy,             CapabilityMaterialsEnum.SpecGlossPbr_Default,    "Resources.WsModelTemplates.MaterialTemplate_wh2_default.xml"),
            new (GameTypeEnum.Warhammer2,       CapabilityMaterialsEnum.SpecGlossPbr_Default,    "Resources.WsModelTemplates.MaterialTemplate_wh2_default.xml"),
            new (GameTypeEnum.Pharaoh,          CapabilityMaterialsEnum.SpecGlossPbr_Default,    "Resources.WsModelTemplates.MaterialTemplate_pharaoh_default.xml"),
             
            // Default - metal rough games
            new (GameTypeEnum.Warhammer3,       CapabilityMaterialsEnum.MetalRoughPbr_Default,   "Resources.WsModelTemplates.MaterialTemplate_wh3_default.xml"),
            new (GameTypeEnum.ThreeKingdoms,    CapabilityMaterialsEnum.MetalRoughPbr_Default,   "Resources.WsModelTemplates.MaterialTemplate_wh3_default.xml"),

            // Emissive 
            new (GameTypeEnum.Warhammer3,       CapabilityMaterialsEnum.MetalRoughPbr_Emissive,  "Resources.WsModelTemplates.MaterialTemplate_wh3_emissive.xml")
        ];

        public GameTypeEnum GameHint { get; private set; }
        private readonly string _templateName;
        private string _templateBuffer;
        
        public WsMaterialTemplateEditor(CapabilityMaterial material, GameTypeEnum gameType)
        {
            GameHint = gameType;

            var template = s_materialToTemplateMap.FirstOrDefault(x=> x.Game == GameHint && x.MaterialType == material.Type);
            if (template == null)
                throw new Exception($"Tryint to create a wsmaterial using {nameof(WsMaterialTemplateEditor)} for {nameof(CapabilityMaterial)} of type {nameof(material.Type)} which is not supported by game {GameHint}. No Template registered");

            _templateName = template.Path;
            _templateBuffer = ResourceLoader.LoadString(_templateName);
        }

        public string GetCompletedMaterialString()
        {
            Verify();
            return _templateBuffer!;
        }

        public string AddTemplateHeader(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            var alphaShaderPart = "";
            var alphaNamePart = "off";
            var fileName = "";

            var baseCapability = capabilityMaterial.TryGetCapability<MaterialBaseCapability>();
            if (baseCapability != null && baseCapability.UseAlpha)
            {
                alphaShaderPart = "_alpha";
                alphaNamePart = "on";
            }
            var materialVertexFormatStr = vertexFormat switch
            {
                UiVertexFormat.Static => "rigid",
                UiVertexFormat.Cinematic => "weighted4",
                UiVertexFormat.Weighted => "weighted2",
                _ => throw new Exception("Unknown vertex type")
            };
            if (GameHint == GameTypeEnum.Pharaoh) //Pharaoh does not require alpha and the naming is different as well as the template
            {
                materialVertexFormatStr = vertexFormat switch
                {
                    UiVertexFormat.Static => "rigid",
                    UiVertexFormat.Cinematic => "weighted_standard_4",
                    UiVertexFormat.Weighted => "weighted_standard_2",
                    _ => throw new Exception("Unknown vertex type")
                };
                fileName = $"{meshName}_{materialVertexFormatStr}.xml";//no alpha attribute in template
                AddAttribute("TEMPLATE_ATTR_FILE_NAME", fileName);
                AddAttribute("TEMPLATE_ATTR_VERTEXTYPE", materialVertexFormatStr);
                return $"{fileName}.material"; ;
            }

            fileName = $"{meshName}_{materialVertexFormatStr}_alpha_{alphaNamePart}.xml";
            AddAttribute("TEMPLATE_ATTR_FILE_NAME", fileName);
            AddAttribute("TEMPLATE_ATTR_ALPHAMODE", alphaShaderPart);
            AddAttribute("TEMPLATE_ATTR_VERTEXTYPE", materialVertexFormatStr);
            return $"{fileName}.material"; ;
        }

        public void AddAttribute(string templateAttributeName, string value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, value);
        }

        public void AddAttribute(string templateAttributeName, bool value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, value ? "1" : "0");
        }

        public void AddAttribute(string templateAttributeName, float value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{Str(value)}");
        }

        public void AddAttribute(string templateAttributeName, Vector2 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{Str(value.X)},{Str(value.Y)}");
        }

        public void AddAttribute(string templateAttributeName, Vector3 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{Str(value.X)},{Str(value.Y)},{Str(value.Z)}");
        }

        public void AddAttribute(string templateAttributeName, Vector4 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{Str(value.X)},{Str(value.Y)},{Str(value.Z)},{Str(value.W)}");
        }

        public void AddAttribute(string templateAttributeName, TextureInput value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");

            if (string.IsNullOrWhiteSpace(value.TexturePath))
                _templateBuffer = _templateBuffer.Replace(templateAttributeName, "test_mask.dds");
            else
                _templateBuffer = _templateBuffer.Replace(templateAttributeName, value.TexturePath);
        }

        void Verify()
        {
            var hasValue = _templateBuffer.Contains("TEMPLATE_ATTR");
            if (hasValue)
            {
                var start = _templateBuffer.IndexOf("TEMPLATE_ATTR");
                var end = _templateBuffer.IndexOf("<", start);
                var attr = _templateBuffer.Substring(start, end - start);

                throw new Exception($"Failed to generate material, not all template attributes are replaced! {attr}");
            }
        }

        string Str(float value, int numDecimals = 4) => value.ToString(CultureInfo.InvariantCulture/*$"F{numDecimals}"*/); 
    }
}
