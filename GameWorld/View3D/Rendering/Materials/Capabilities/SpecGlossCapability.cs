using System;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class SpecGlossCapability : MaterialBaseCapability
    {
        public TextureInput SpecularMap { get; set; } = new TextureInput(TextureType.Specular);
        public TextureInput GlossMap { get; set; } = new TextureInput(TextureType.Gloss);
        public TextureInput DiffuseMap { get; set; } = new TextureInput(TextureType.Diffuse);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);

        public override void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {
            SpecularMap.Apply(effect, resourceLibrary);
            GlossMap.Apply(effect, resourceLibrary);
            DiffuseMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);

            base.Apply(effect, resourceLibrary);    
        }

        public override ICapability Clone()
        {
            return new SpecGlossCapability()
            {
                ScaleMult = ScaleMult,
                UseAlpha = UseAlpha,
                SpecularMap = SpecularMap.Clone(),
                GlossMap = GlossMap.Clone(),
                DiffuseMap = DiffuseMap.Clone(),
                NormalMap = NormalMap.Clone(),
                Mask = Mask.Clone(),
            };
        }

        public override void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)=> SpecGlossCapabilitySerializer.Initialize(this, wsModelMaterial, rmvMaterial);
        public override void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) => SpecGlossCapabilitySerializer.SerializeToWsModel(this, templateHandler);
        public override void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) => SpecGlossCapabilitySerializer.SerializeToRmvMaterial(this, rmvMaterial);

        public override (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            var typedCap = otherCap as SpecGlossCapability;
            if (typedCap == null)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(SpecularMap, typedCap.SpecularMap, nameof(SpecularMap), out var res0))
                return res0;
            if (!CompareHelper.Compare(GlossMap, typedCap.GlossMap, nameof(GlossMap), out var res1))
                return res1;
            if (!CompareHelper.Compare(DiffuseMap, typedCap.DiffuseMap, nameof(DiffuseMap), out var res2))
                return res2;
            if (!CompareHelper.Compare(NormalMap, typedCap.NormalMap, nameof(NormalMap), out var res3))
                return res3;
            if (!CompareHelper.Compare(Mask, typedCap.Mask, nameof(Mask), out var res4))
                return res4;

            return base.AreEqual(otherCap);
        }
    }

    public static class SpecGlossCapabilitySerializer
    {
        public static void Initialize(SpecGlossCapability output, WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            if (wsModelMaterial != null)
            {
                output.UseAlpha = wsModelMaterial.Alpha;
            }
            else
            {
                if (rmvMaterial is WeightedMaterial weightedMaterial == false)
                    throw new Exception($"Unable to convert material of type {rmvMaterial?.MaterialId} into {nameof(WeightedMaterial)}");

                weightedMaterial.IntParams.TryGet(WeightedParamterIds.IntParams_Alpha_index, out var useAlpha);
                output.UseAlpha = useAlpha == 1;
            }

            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.SpecularMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.GlossMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.DiffuseMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.Mask);

   
        }

        public static void SerializeToWsModel(SpecGlossCapability typedCap, WsMaterialTemplateEditor templateHandler)
        {


            templateHandler.AddAttribute(WsModelParamters.Texture_Specular.TemplateName, typedCap.SpecularMap);
            if (templateHandler.GameHint != GameTypeEnum.Pharaoh)
                templateHandler.AddAttribute(WsModelParamters.Texture_Gloss.TemplateName, typedCap.GlossMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Diffse.TemplateName, typedCap.DiffuseMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Normal.TemplateName, typedCap.NormalMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Mask.TemplateName, typedCap.Mask);
        }

        public static void SerializeToRmvMaterial(SpecGlossCapability typedCap, IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()}' is not {nameof(WeightedMaterial)} - Unable to serialize to rmv");
            rmvMaterial.SetTexture(typedCap.SpecularMap.Type, typedCap.SpecularMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.GlossMap.Type, typedCap.GlossMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.DiffuseMap.Type, typedCap.DiffuseMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.NormalMap.Type, typedCap.NormalMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.Mask.Type, typedCap.Mask.TexturePath);

            weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Alpha_index, typedCap.UseAlpha ? 1 : 0);
        }
    }
}
