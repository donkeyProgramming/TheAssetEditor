using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Shaders;
using Microsoft.Xna.Framework;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials
{
    public class MaterialToWsModelSerializer : IMaterialToWsModelSerializer
    {
        static readonly Dictionary<CapabilityMaterialsEnum, string> s_materialToTemplateMap = new()
        {
            { CapabilityMaterialsEnum.MetalRoughPbr_Default,    "Resources.WsModelTemplates.MaterialTemplate_wh3_default.xml.material"},
          //  { CapabilityMaterialsEnum.MetalRoughPbr_Emissive,   "Resources.WsModelTemplates.MaterialTemplate_wh3_emissive.xml.material"}
        };

        public MaterialToWsModelSerializer(GameTypeEnum preferedGameHint = GameTypeEnum.Unknown)
        { }

        public (string FileName, string FileContent) Create(string uniqueMeshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            var isTemplateSupported = s_materialToTemplateMap.TryGetValue(capabilityMaterial.Type, out var templateName);
            if (isTemplateSupported == false)
                throw new Exception($"Tryint to create a wsmaterial using {nameof(MaterialToWsModelSerializer)} for {nameof(CapabilityMaterial)} of type {nameof(capabilityMaterial.Type)} which is not supported");

            LoadTemplate(templateName!);
            var fileName = AddShaderName(uniqueMeshName, vertexFormat, capabilityMaterial);

            foreach (var cap in capabilityMaterial.Capabilities)
                cap.SerializeToWsModel(this);


            //AddDefault(capabilityMaterial.GetCapability<DefaultCapabilityMetalRough>());
            //AddBlood(capabilityMaterial.TryGetCapability<BloodCapability>());
            //AddTint(capabilityMaterial.TryGetCapability<TintCapability>());
            //AddEmissive(capabilityMaterial.TryGetCapability<EmissiveCapability>());

            Verify();
            return ($"{fileName}.material", _templateBuffer!);
        }

        string AddShaderName(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            var alphaShaderPart = "";
            var alphaNamePart = "off";
            if (capabilityMaterial.GetCapability<DefaultCapabilityMetalRough>().UseAlpha)
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
            Add("TEMPLATE_ATTR_FILE_NAME", fileName);
            Add("TEMPLATE_ATTR_ALPHAMODE", alphaShaderPart);
            Add("TEMPLATE_ATTR_VERTEXTYPE", materialVertexFormatStr);
            return fileName;
        }

        void AddDefault(DefaultCapabilityMetalRough defaultCapability)
        {
            Add("TEMPLATE_ATTR_BASE_COLOUR_PATH", defaultCapability.BaseColour);
            Add("TEMPLATE_ATTR_MASK_PATH", defaultCapability.Mask);
            Add("TEMPLATE_ATTR_MATERIAL_MAP", defaultCapability.MaterialMap);
            Add("TEMPLATE_ATTR_NORMAL_PATH", defaultCapability.NormalMap);
            Add("TEMPLATE_ATTR_DISTORTION_PATH", defaultCapability.Distortion);
            Add("TEMPLATE_ATTR_DISTORTIONNOISE_PATH", defaultCapability.DistortionNoise);
        }

        void AddBlood(BloodCapability? blood)
        {
            if (blood == null)
                return;

            Add("TEMPLATE_ATTR_BLOOOD_PATH", blood.BloodMask);
            Add("TEMPLATE_ATTR_BLOOD_UV_SCALE_VALUE", blood.UvScale);
            Add("TEMPLATE_ATTR_USE_BLOOD_VALUE", blood.UseBlood ? 1 : 0);
        }

        void AddTint(TintCapability? tint)
        {
            //if (tint == null)
            //    return;
            //
            //Add("Tempalte_tintColour", tint.Faction2_TintVariation);
            //Add("Tempalte_tintGradient0", tint.Faction2_TintVariation);
            //Add("Tempalte_tintGradientTime", tint.Faction2_TintVariation);
        }

        void AddEmissive(EmissiveCapability? emissive)
        {
            //if (emissive == null)
            //    return;
            //
            //Add("Tempalte_Texture", emissive.Emissive);
            ////Add("Tempalte_tintGradient0", tint.Faction2_TintVariation);
            ////Add("Tempalte_tintGradientTime", tint.Faction2_TintVariation);
        }

        protected string? _templateBuffer;
        private string? _templateName;



        protected void LoadTemplate(string templatePath)
        {
            _templateName = templatePath;
            _templateBuffer = ResourceLoader.LoadString(templatePath);
        }

        protected void Add(string templateAttributeName, string value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value);
        }

        protected void Add(string templateAttributeName, float value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value.ToString());
        }

        protected void Add(string templateAttributeName, Vector2 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}");
        }

        protected void Add(string templateAttributeName, Vector3 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}");
        }

        protected void Add(string templateAttributeName, Vector4 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}, {value.W}");
        }

        protected void Add(string templateAttributeName, TextureInput value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");

            if (string.IsNullOrWhiteSpace(value.TexturePath))
                _templateBuffer = _templateBuffer!.Replace(templateAttributeName, "test_mask.dds");
            else
                _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value.TexturePath);
        }

        protected void Verify()
        {
            var hasValue = _templateBuffer!.Contains("TEMPLATE_ATTR");
            if (hasValue)
                throw new Exception("Failed to generate material, not all template attributes are replaced!");
        }
    }


}
