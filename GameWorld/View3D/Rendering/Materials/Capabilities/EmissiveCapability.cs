using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class EmissiveCapability : ICapability
    {
        public TextureInput Emissive { get; set; } = new TextureInput(TextureType.Emissive);
        public TextureInput EmissiveDistortion { get; set; } = new TextureInput(TextureType.EmissiveDistortion);

        public Vector2 EmissiveDirection { get; set; } = new Vector2(1);

        public float EmissiveDistortStrength { get; set; } = 1;
        public float EmissiveFresnelStrength { get; set; } = 1;

        public float EmissiveSpeed { get; set; } = 1;
        public float EmissivePulseSpeed { get; set; } = 1;
        public float EmissivePulseStrength { get; set; } = 1;

        // Colents, where the Alpha is used as time (0-1). RedGreenBlueTime
        public Vector3[] GradientColours { get; set; } = [new Vector3(0), new Vector3(0.25f), new Vector3(0.75f), new Vector3(1)];
        public float[] GradientTimes { get; set; } = [0, 0.25f, 0.75f, 1];

        public float EmissiveStrength { get; set; } = 1;
        public Vector2 EmissiveTiling { get; set; } = new Vector2(1);
        public Vector3 EmissiveTint { get; set; } = new Vector3(1, 0, 0);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            Emissive.Apply(effect, resourceLibrary);

            effect.Parameters["CapabilityFlag_ApplyEmissive"].SetValue(true);

            effect.Parameters["Emissive_Tint"].SetValue(EmissiveTint);
            effect.Parameters["Emissive_Strength"].SetValue(EmissiveStrength);
            effect.Parameters["Emissive_Tiling"].SetValue(EmissiveTiling);
            effect.Parameters["Emissive_GradientColours"].SetValue(GradientColours);
            effect.Parameters["Emissive_GradientTimes"].SetValue(GradientTimes);
            effect.Parameters["Emissive_FresnelStrength"].SetValue(EmissiveFresnelStrength);
        }

        public ICapability Clone()
        {
            return new EmissiveCapability()
            {
                Emissive = Emissive.Clone(),
                EmissiveDistortion = EmissiveDistortion.Clone(),
                EmissiveDirection = EmissiveDirection,
                EmissiveDistortStrength = EmissiveDistortStrength,
                EmissiveFresnelStrength = EmissiveFresnelStrength,
                EmissiveSpeed = EmissiveSpeed,
                EmissivePulseSpeed = EmissivePulseSpeed,
                EmissivePulseStrength = EmissivePulseStrength,
                GradientColours = [GradientColours[0], GradientColours[1], GradientColours[2], GradientColours[3]],
                GradientTimes = [GradientTimes[0], GradientTimes[1], GradientTimes[2], GradientTimes[3]],
                EmissiveStrength = EmissiveStrength,
                EmissiveTiling = EmissiveTiling,
                EmissiveTint = EmissiveTint,
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Emissive);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, EmissiveDistortion);

            for (var i = 0; i < 4; i++)
            {
                GradientColours[i] = CapabilityHelper.GetParameterVector3(wsModelMaterial, "emissive_gradient_colour_stop_" + (i + 1), Vector3.One);
                GradientTimes[i] = CapabilityHelper.GetParameterFloat(wsModelMaterial, "emissive_gradient_stop_" + (i + 1), 0);
            }

            EmissiveStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, "emissive_strength", 1);
            EmissiveTint = CapabilityHelper.GetParameterVector3(wsModelMaterial, "emissive_tint", Vector3.Zero);
            EmissiveFresnelStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, "emissive_fresnel_strength", 1);
        }
    }
}
