using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
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

        public override void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, BaseColour);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, MaterialMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Mask);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Distortion, "commontextures/winds_of_magic_specular.dds");
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, DistortionNoise, "commontextures/winds_of_magic_noise.dds");

            base.Initialize(wsModelMaterial, rmvMaterial);
        }

        public override void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
            templateHandler.AddAttribute(WsModelParamters.Texture_BaseColour.TemplateName, BaseColour);
            templateHandler.AddAttribute(WsModelParamters.Texture_Mask.TemplateName, Mask);
            templateHandler.AddAttribute(WsModelParamters.Texture_MaterialMap.TemplateName, MaterialMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Normal.TemplateName, NormalMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Distortion.TemplateName, Distortion);
            templateHandler.AddAttribute(WsModelParamters.Texture_DistortionNoise.TemplateName, DistortionNoise);

            base.SerializeToWsModel(templateHandler);
        }

        public override void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) 
        {
            rmvMaterial.SetTexture(BaseColour.Type, BaseColour.TexturePath);
            rmvMaterial.SetTexture(MaterialMap.Type, MaterialMap.TexturePath);
            rmvMaterial.SetTexture(NormalMap.Type, NormalMap.TexturePath);
            rmvMaterial.SetTexture(Mask.Type, Mask.TexturePath);

            base.SerializeToRmvMaterial(rmvMaterial);
        }

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
}
