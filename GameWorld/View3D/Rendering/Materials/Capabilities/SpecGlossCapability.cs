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


        public override void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, SpecularMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, GlossMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, DiffuseMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Mask);

            base.Initialize(wsModelMaterial, rmvMaterial);
        }

        public override void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
            
            templateHandler.AddAttribute(WsModelParamters.Texture_Specular.TemplateName, SpecularMap);
            if (templateHandler.GameHint != GameTypeEnum.Pharaoh)
                templateHandler.AddAttribute(WsModelParamters.Texture_Gloss.TemplateName, GlossMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Diffse.TemplateName, DiffuseMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Normal.TemplateName, NormalMap);
            templateHandler.AddAttribute(WsModelParamters.Texture_Mask.TemplateName, Mask);

            base.SerializeToWsModel(templateHandler);
        }

        public override void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            rmvMaterial.SetTexture(SpecularMap.Type, SpecularMap.TexturePath);
            rmvMaterial.SetTexture(GlossMap.Type, GlossMap.TexturePath);
            rmvMaterial.SetTexture(DiffuseMap.Type, DiffuseMap.TexturePath);
            rmvMaterial.SetTexture(NormalMap.Type, NormalMap.TexturePath);
            rmvMaterial.SetTexture(Mask.Type, Mask.TexturePath);

            base.SerializeToRmvMaterial (rmvMaterial);
        }

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
}
