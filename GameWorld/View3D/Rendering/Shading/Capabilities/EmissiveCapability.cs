using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
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
        public Vector4[] Gradient { get; set; } = [new Vector4(0), new Vector4(0.25f), new Vector4(0.75f), new Vector4(1)];

        public float EmissiveStrength { get; set; } = 1;
        public Vector2 EmissiveTiling { get; set; } = new Vector2(1);
        public Vector3 EmissiveTint { get; set; } = new Vector3(1, 0, 0);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
           
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
                Gradient = [Gradient[0], Gradient[1], Gradient[2], Gradient[3]],
                EmissiveStrength = EmissiveStrength,
                EmissiveTiling = EmissiveTiling,
                EmissiveTint = EmissiveTint,
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, Emissive);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, EmissiveDistortion);

            for (var i = 0; i < 4; i++)
            {
                var colour = CapabilityHelper.GetParameterVector3(wsModelMaterial, "emissive_gradient_colour_stop_" + (i + 1), Vector3.One);
                var time = CapabilityHelper.GetParameterFloat(wsModelMaterial, "emissive_gradient_stop_" + (i + 1), 0);
                Gradient[i] = new Vector4(colour, time);
            }

            EmissiveStrength = CapabilityHelper.GetParameterFloat(wsModelMaterial, "emissive_strength", 1);
            EmissiveTint = CapabilityHelper.GetParameterVector3(wsModelMaterial, "emissive_tint", Vector3.Zero);

         
        }
    }
}
