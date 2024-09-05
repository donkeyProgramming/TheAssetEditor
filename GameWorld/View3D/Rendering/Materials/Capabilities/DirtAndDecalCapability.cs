using System;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class DirtAndDecalCapability : ICapability
    {
        public TextureInput DirtMap { get; set; } = new TextureInput(TextureType.Decal_dirtmap);
        public TextureInput DirtMask { get; set; } = new TextureInput(TextureType.Decal_dirtmask);
        public TextureInput DecalMask { get; set; } = new TextureInput(TextureType.Decal_mask);

        public Vector2 UvScale { get; set; } = new Vector2(2);
        public Vector4 TextureTransform { get; set; } = new Vector4(0.0333f, 0.236f, 0.03943f, 0.952f);

        public TextureInput DecalPreviewColour { get; set; } = new TextureInput(TextureType.Decal_mask);
        public TextureInput DecalPreviewNormal { get; set; } = new TextureInput(TextureType.Decal_mask);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {

        }

        public ICapability Clone()
        {
            return new DirtAndDecalCapability()
            {
                DirtMap = DirtMap.Clone(),
                DirtMask = DirtMask.Clone(),
                DecalMask = DecalMask.Clone(),
                UvScale = UvScale,
                TextureTransform = TextureTransform
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()} - {rmvMaterial.MaterialId}' is not {nameof(WeightedMaterial)}, and can not used to create a {nameof(DirtAndDecalCapability)}");

            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DirtMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DirtMask);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DecalMask);

            UvScale = new Vector2(weightedMateial.FloatParams[0], weightedMateial.FloatParams[1]);
            TextureTransform = weightedMateial.Vec4Params[0].ToVector4();
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is not WeightedMaterial weightedMateial)
                throw new Exception($"Input material '{rmvMaterial.GetType()}' is not {nameof(WeightedMaterial)}, and can not used to create a {nameof(DirtAndDecalCapability)}");

            weightedMateial.IsDirtAndDecal = true;

            weightedMateial.SetTexture(DirtMap.Type, DirtMap.TexturePath);
            weightedMateial.SetTexture(DirtMask.Type, DirtMask.TexturePath);
            weightedMateial.SetTexture(DecalMask.Type, DecalMask.TexturePath);

            if (weightedMateial.Vec4Params.Count == 0)
                weightedMateial.Vec4Params.Add(new RmvVector4());
            weightedMateial.Vec4Params[0] = new RmvVector4(TextureTransform.X, TextureTransform.Y, TextureTransform.Z, TextureTransform.W);

            if (weightedMateial.FloatParams.Count == 0)
                weightedMateial.FloatParams.Add(0);
            if (weightedMateial.FloatParams.Count == 1)
                weightedMateial.FloatParams.Add(0);

            weightedMateial.FloatParams[0] = UvScale.X;
            weightedMateial.FloatParams[1] = UvScale.Y;
        }

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not DirtAndDecalCapability typedCap)
                throw new Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(DirtMap, typedCap.DirtMap, nameof(DirtMap), out var res0))
                return res0;

            if (!CompareHelper.Compare(DirtMask, typedCap.DirtMask, nameof(DirtMask), out var res1))
                return res1;

            if (!CompareHelper.Compare(DecalMask, typedCap.DecalMask, nameof(DirtMask), out var res2))
                return res2;

            if (!CompareHelper.Compare(UvScale, typedCap.UvScale, nameof(UvScale), out var res3))
                return res3;

            if (!CompareHelper.Compare(TextureTransform, typedCap.TextureTransform, nameof(TextureTransform), out var res4))
                return res4;

            return (true, "");
        }
    }
}
