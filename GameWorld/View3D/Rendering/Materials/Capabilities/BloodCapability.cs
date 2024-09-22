﻿using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class BloodCapability : ICapability
    {
        public bool UseBlood { get; set; } = true;
        public Vector2 UvScale { get; set; } = new Vector2(1);
        public TextureInput BloodMask { get; set; } = new TextureInput(TextureType.Blood);
        public float PreviewBlood { get; set; } = 0;

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
        }

        public ICapability Clone()
        {
            return new BloodCapability()
            {
                UseBlood = UseBlood,
                BloodMask = BloodMask.Clone(),
                PreviewBlood = PreviewBlood,
                UvScale = UvScale
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, BloodMask, "commontextures/bloodmap.dds");
            UseBlood = CapabilityHelper.GetParameterFloat(wsModelMaterial, WsModelParamters.Blood_Use, 0) == 1;
            UvScale = CapabilityHelper.GetParameterVector2(wsModelMaterial, WsModelParamters.Blood_Scale, Vector2.One);
        }

        public void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
            templateHandler.AddAttribute(WsModelParamters.Texture_Blood.TemplateName, BloodMask);                                             
            templateHandler.AddAttribute(WsModelParamters.Blood_Scale.TemplateName, UvScale);
            templateHandler.AddAttribute(WsModelParamters.Blood_Use.TemplateName, UseBlood);
        }

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not BloodCapability typedCap)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(UseBlood, typedCap.UseBlood, nameof(UseBlood), out var res0))
                return res0;

            if (!CompareHelper.Compare(BloodMask, typedCap.BloodMask, nameof(BloodMask), out var res1))
                return res1;

            if (!CompareHelper.Compare(UvScale, typedCap.UvScale, nameof(UvScale), out var res2))
                return res2;


            return (true, "");
        }
    }
}
