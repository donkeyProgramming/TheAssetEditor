using System.Numerics;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    public class TintCapability : ICapability
    {
        public Vector3 DiffuseTintMask { get; set; } = Vector3.Zero;
        public Vector3 DiffuseTintColour { get; set; } = Vector3.Zero;
        public float DiffuseTintVariation { get; set; }

        public Vector3 Faction3Mask { get; set; } = Vector3.Zero;
        public float Faction1_TintVariation { get; set; } = 0;  //Replace as vector3?
        public float Faction2_TintVariation { get; set; } = 0;
        public float Faction3_TintVariation { get; set; } = 0;

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
        }
    }
}
