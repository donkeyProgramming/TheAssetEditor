﻿using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.WpfWindow.ResourceHandling;
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
            templateHandler.AddAttribute(WsModelParamters.Blood_Use.TemplateName, UseBlood ? 1 : 0);
        }
    }
}
