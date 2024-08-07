using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.Rendering.Shading.Shaders;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{

    public class CapabilityMaterialBuilderWarhammer3 : IWsMaterialBuilder
    {
        static readonly Dictionary<CapabilityMaterialsEnum, string> s_materialToTemplateMap = new()
        {
            { CapabilityMaterialsEnum.MetalRoughPbr_Default, "Resources.WsModelTemplates.MaterialTemplate_wh3.xml.material"},
            { CapabilityMaterialsEnum.MetalRoughPbr_Emissive, "Resources.WsModelTemplates.MaterialTemplate_wh3.xml.material"}
        };

        public override (string FileName, string FileContent) Create(string uniqueMeshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            var isTemplateSupported = s_materialToTemplateMap.TryGetValue(capabilityMaterial.Type, out var templateName);
            if (isTemplateSupported == false)
                throw new Exception($"Tryint to create a wsmaterial using {nameof(CapabilityMaterialBuilderWarhammer3)} for {nameof(CapabilityMaterial)} of type {nameof(capabilityMaterial.Type)} which is not supported");

            LoadTemplate(templateName!);
            AddShaderName(uniqueMeshName, vertexFormat, capabilityMaterial);

            AddDefault(capabilityMaterial.GetCapability<DefaultCapability>());
            AddBlood(capabilityMaterial.TryGetCapability<BloodCapability>());
            AddTint(capabilityMaterial.TryGetCapability<TintCapability>());
            AddEmissive(capabilityMaterial.TryGetCapability<EmissiveCapability>());

            return ("SomeFileName.xml.material", _templateBuffer!);
        }

        void AddShaderName(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            var alphaShaderPart = "";
            var alphaNamePart = "off";
            if (capabilityMaterial.GetCapability<DefaultCapability>().UseAlpha)
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

            Add("TEMPLATE_ATTR_FILE_NAME", $"{meshName}_{materialVertexFormatStr}_alpha_{alphaNamePart}.xml");
            Add("TEMPLATE_ATTR_ALPHAMODE", alphaShaderPart);
            Add("TEMPLATE_ATTR_VERTEXTYPE", materialVertexFormatStr);
        }

        void AddDefault(DefaultCapability defaultCapability)
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
    }

    public class CapabilityMaterialBuilderPharaoh : IWsMaterialBuilder
    {
        public override string Create(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            return "";
        }
    }

    public class CapabilityMaterialBuilderWarhammer2 : IWsMaterialBuilder
    {
        public override string Create(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial)
        {
            return "";
        }
    }
}
