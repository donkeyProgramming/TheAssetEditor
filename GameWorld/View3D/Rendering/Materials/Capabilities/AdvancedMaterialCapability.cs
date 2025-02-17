using System;
using System.Threading.Tasks;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class AdvancedMaterialCapability : ICapability
    {
        public bool UseDirt { get; set; } = true;
        public bool UseDecal { get; set; } = false;
        public bool UseSkin { get; set; } = false;

        public TextureInput DirtMap { get; set; } = new TextureInput(TextureType.Decal_dirtmap);
        public TextureInput DirtMask { get; set; } = new TextureInput(TextureType.Decal_dirtmask);
        public TextureInput DecalMask { get; set; } = new TextureInput(TextureType.Decal_mask);
        public TextureInput SkinMask { get; set; } = new TextureInput(TextureType.Skin_mask);

        public Vector2 UvScale { get; set; } = new Vector2(1);
        public Vector4 TextureTransform { get; set; } = new Vector4(0, 0, 0, 0);

        public void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {

        }

        public ICapability Clone()
        {
            return new AdvancedMaterialCapability()
            {
                DirtMap = DirtMap.Clone(),
                DirtMask = DirtMask.Clone(),
                DecalMask = DecalMask.Clone(),
                SkinMask = SkinMask.Clone(),
                UseDecal = UseDecal,
                UseDirt = UseDirt,
                UseSkin = UseSkin,
                UvScale = UvScale,
                TextureTransform = TextureTransform
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()} - {rmvMaterial.MaterialId}' is not {nameof(WeightedMaterial)}, and can not used to create a {nameof(AdvancedMaterialCapability)}");

            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DirtMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DirtMask);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DecalMask);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, SkinMask);

            UseDirt = RmvMaterialUtil.IsDirt(rmvMaterial);
            UseDecal= RmvMaterialUtil.IsDecal(rmvMaterial);
            UseSkin = RmvMaterialUtil.IsSkin(rmvMaterial);

            if (UseDirt)
            {
                var uvScaleX = weightedMateial.FloatParams.Get(WeightedParamterIds.FloatParams_UvScaleX);
                var uvScaleY = weightedMateial.FloatParams.Get(WeightedParamterIds.FloatParams_UvScaleX);
                UvScale = new Vector2(uvScaleX, uvScaleY);
            }

            if (UseDecal)
                TextureTransform = weightedMateial.Vec4Params.Get(WeightedParamterIds.Vec4Params_TextureDecalTransform).ToVector4(); 
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()}' is not {nameof(WeightedMaterial)}, and can not used to create a {nameof(AdvancedMaterialCapability)}");

            weightedMateial.MaterialHint = RmvMaterialUtil.GetMaterialHint(UseDirt, UseDecal, UseSkin);
            switch (weightedMateial.MaterialHint)
            {
                case WeightedMaterial.MaterialHintEnum.Dirt:
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Dirt_index, 1);
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Decal_index, 1);

                    weightedMateial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleX, UvScale.X);
                    weightedMateial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleY, UvScale.Y);

                    rmvMaterial.SetTexture(DirtMap.Type, DirtMap.TexturePath);
                    rmvMaterial.SetTexture(DirtMask.Type, DirtMask.TexturePath);
                    break;

                case WeightedMaterial.MaterialHintEnum.Decal_Dirt:
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Dirt_index, 1);
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Decal_index, 1);

                    weightedMateial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleX, UvScale.X);
                    weightedMateial.FloatParams.Set(WeightedParamterIds.FloatParams_UvScaleY, UvScale.Y);

                    weightedMateial.Vec4Params.Set(WeightedParamterIds.Vec4Params_TextureDecalTransform, new RmvVector4(TextureTransform.X, TextureTransform.Y, TextureTransform.Z, TextureTransform.W));

                    rmvMaterial.SetTexture(DirtMap.Type, DirtMap.TexturePath);
                    rmvMaterial.SetTexture(DirtMask.Type, DirtMask.TexturePath);
                    rmvMaterial.SetTexture(DecalMask.Type, DecalMask.TexturePath);
                    break;
    
                case WeightedMaterial.MaterialHintEnum.Decal:
                    weightedMateial.Vec4Params.Set(WeightedParamterIds.Vec4Params_TextureDecalTransform, new RmvVector4(TextureTransform.X, TextureTransform.Y, TextureTransform.Z, TextureTransform.W));
                    rmvMaterial.SetTexture(DecalMask.Type, DecalMask.TexturePath);
                    break;

                case WeightedMaterial.MaterialHintEnum.Skin:
                    break;

                case WeightedMaterial.MaterialHintEnum.Skin_Dirt:
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Dirt_index, 1);
                    weightedMateial.IntParams.Set(WeightedParamterIds.IntParams_Decal_index, 1);

                    rmvMaterial.SetTexture(DirtMap.Type, DirtMap.TexturePath);
                    rmvMaterial.SetTexture(DirtMask.Type, DirtMask.TexturePath);
                    rmvMaterial.SetTexture(SkinMask.Type, SkinMask.TexturePath);
                    break;
            }
        }

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not AdvancedMaterialCapability typedCap)
                throw new Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(DirtMap, typedCap.DirtMap, nameof(DirtMap), out var res0))
                return res0;

            if (!CompareHelper.Compare(DirtMask, typedCap.DirtMask, nameof(DirtMask), out var res1))
                return res1;

            if (!CompareHelper.Compare(DecalMask, typedCap.DecalMask, nameof(DirtMask), out var res2))
                return res2;

            if (!CompareHelper.Compare(SkinMask, typedCap.SkinMask, nameof(SkinMask), out var res_skin))
                return res_skin;

            if (!CompareHelper.Compare(UvScale, typedCap.UvScale, nameof(UvScale), out var res3))
                return res3;

            if (!CompareHelper.Compare(TextureTransform, typedCap.TextureTransform, nameof(TextureTransform), out var res4))
                return res4;

            if (!CompareHelper.Compare(UseDecal, typedCap.UseDecal, nameof(TextureTransform), out var res5))
                return res5;

            if (!CompareHelper.Compare(UseDirt, typedCap.UseDirt, nameof(TextureTransform), out var res6))
                return res6;

            if (!CompareHelper.Compare(UseSkin, typedCap.UseSkin, nameof(TextureTransform), out var res7))
                return res7;

            return (true, "");
        }
    }
}
