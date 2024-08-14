using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Shaders;
using Microsoft.Xna.Framework;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class WsMaterialTemplateEditor 
    {
        static readonly Dictionary<CapabilityMaterialsEnum, string> s_materialToTemplateMap = new()
        {
            { CapabilityMaterialsEnum.MetalRoughPbr_Default,    "Resources.WsModelTemplates.MaterialTemplate_wh3_default.xml.material"},
            { CapabilityMaterialsEnum.MetalRoughPbr_Emissive,   "Resources.WsModelTemplates.MaterialTemplate_wh3_emissive.xml.material"}
        };

        private readonly GameTypeEnum _preferedGameHint;
        private readonly string _templateName;
        private string _templateBuffer;
        
        public WsMaterialTemplateEditor(CapabilityMaterial material, GameTypeEnum preferedGameHint = GameTypeEnum.Unknown)
        {
            _preferedGameHint = preferedGameHint;

            var isTemplateSupported = s_materialToTemplateMap.TryGetValue(material.Type, out var templateName);
            if (isTemplateSupported == false)
                throw new Exception($"Tryint to create a wsmaterial using {nameof(WsMaterialTemplateEditor)} for {nameof(CapabilityMaterial)} of type {nameof(material.Type)} which is not supported. No Template registered");

            _templateName = templateName!;
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

            var fileName = $"{meshName}_{materialVertexFormatStr}_alpha_{alphaNamePart}.xml";
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

        public void AddAttribute(string templateAttributeName, float value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, value.ToString());
        }

        public void AddAttribute(string templateAttributeName, Vector2 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{value.X}, {value.Y}");
        }

        public void AddAttribute(string templateAttributeName, Vector3 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}");
        }

        public void AddAttribute(string templateAttributeName, Vector4 value)
        {
            if (_templateBuffer.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}, {value.W}");
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
                throw new Exception("Failed to generate material, not all template attributes are replaced!");
        }

    }
}
