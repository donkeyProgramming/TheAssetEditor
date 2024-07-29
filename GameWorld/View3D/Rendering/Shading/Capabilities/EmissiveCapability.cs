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

        // Colour gradients, where the Alpha is used as time (0-1). RedGreenBlueTime
        public Vector4 Gradient0 { get; set; } = new Vector4(0);
        public Vector4 Gradient1 { get; set; } = new Vector4(0.25f);
        public Vector4 Gradient2 { get; set; } = new Vector4(0.75f);
        public Vector4 Gradient3 { get; set; } = new Vector4(1);

        public float EmissiveStrength { get; set; } = 1;
        public Vector2 EmissiveTiling { get; set; } = new Vector2(1);
        public Vector3 EmissiveTint { get; set; } = new Vector3(1, 0, 0);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
           
        }

        public void Initialize(WsModelFile wsModelFile, RmvModel model)
        {
            
        }
    }
}
