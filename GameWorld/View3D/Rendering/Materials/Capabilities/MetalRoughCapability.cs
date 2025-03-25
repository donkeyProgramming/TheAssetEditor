using System;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class MetalRoughCapability : MaterialBaseCapability
    {
        public TextureInput BaseColour { get; set; } = new TextureInput(TextureType.BaseColour);
        public TextureInput MaterialMap { get; set; } = new TextureInput(TextureType.MaterialMap);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);
        public TextureInput Distortion { get; set; } = new TextureInput(TextureType.Distortion);
        public TextureInput DistortionNoise { get; set; } = new TextureInput(TextureType.DistortionNoise);

        public override void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {
            BaseColour.Apply(effect, resourceLibrary);
            MaterialMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            Mask.Apply(effect, resourceLibrary);

            base.Apply(effect, resourceLibrary);
        }

        public override ICapability Clone()
        {
            return new MetalRoughCapability()
            {
                ScaleMult = ScaleMult,
                UseAlpha = UseAlpha,
                BaseColour = BaseColour.Clone(),
                MaterialMap = MaterialMap.Clone(),
                NormalMap = NormalMap.Clone(),
                Mask = Mask.Clone(),
                Distortion = Distortion.Clone(),
                DistortionNoise = DistortionNoise.Clone(),
            };
        }

        public override void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial) => MetalRoughCapabilitySerializer.Initialize(this, wsModelMaterial, rmvMaterial);

        public override void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) => MetalRoughCapabilitySerializer.SerializeToWsModel(this, templateHandler);

        public override void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) => MetalRoughCapabilitySerializer.SerializeToRmvMaterial(this, rmvMaterial);

        public override (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not MetalRoughCapability typedCap)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(BaseColour, typedCap.BaseColour, nameof(BaseColour), out var res0))
                return res0;
            if (!CompareHelper.Compare(MaterialMap, typedCap.MaterialMap, nameof(MaterialMap), out var res1))
                return res1;
            if (!CompareHelper.Compare(NormalMap, typedCap.NormalMap, nameof(NormalMap), out var res2))
                return res2;
            if (!CompareHelper.Compare(Mask, typedCap.Mask, nameof(Mask), out var res3))
                return res3;

            return base.AreEqual(otherCap);
        }
    }

    public static class MetalRoughCapabilitySerializer
    {
        public static void Initialize(MetalRoughCapability output, WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
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

            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.BaseColour);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.MaterialMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.Mask);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.Distortion, "commontextures/winds_of_magic_specular.dds");
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, output.DistortionNoise, "commontextures/winds_of_magic_noise.dds");

       
        }

        public static void SerializeToWsModel(MetalRoughCapability typedCap, WsMaterialTemplateEditor templateHandler)
        {


            templateHandler.AddAttribute(WsModelParamters.Texture_BaseColour.TemplateName, typedCap.BaseColour);
            templateHandler.AddAttribute(WsModelParamters.Texture_Mask.TemplateName, typedCap.Mask);
            templateHandler.AddAttribute(WsModelParamters.Texture_MaterialMap.TemplateName, typedCap.MaterialMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Normal.TemplateName, typedCap.NormalMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Distortion.TemplateName, typedCap.Distortion);
            templateHandler.AddAttribute(WsModelParamters.Texture_DistortionNoise.TemplateName, typedCap.DistortionNoise);
        }

        public static void SerializeToRmvMaterial(MetalRoughCapability typedCap, IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()}' is not {nameof(WeightedMaterial)} - Unable to serialize to rmv");

            rmvMaterial.SetTexture(typedCap.BaseColour.Type, typedCap.BaseColour.TexturePath);
            rmvMaterial.SetTexture(typedCap.MaterialMap.Type, typedCap.MaterialMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.NormalMap.Type, typedCap.NormalMap.TexturePath);
            rmvMaterial.SetTexture(typedCap.Mask.Type, typedCap.Mask.TexturePath);

            weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Alpha_index, typedCap.UseAlpha ? 1 : 0);
        }
    }
}
