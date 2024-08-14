using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class TintCapability : ICapability
    {
        public bool ApplyCapability { get; set; } = true;

        public Vector4 DiffuseTintMask { get; set; } = Vector4.Zero;
        public Vector3 DiffuseTintColour { get; set; } = Vector3.Zero;
        public float DiffuseTintVariation { get; set; }

        public bool UseFactionColours { get; set; } = true;
        public Vector4 Faction3Mask { get; set; } = Vector4.Zero;
        public float Faction1_TintVariation { get; set; } = 0;  //Replace as vector3?
        public float Faction2_TintVariation { get; set; } = 0;
        public float Faction3_TintVariation { get; set; } = 0;
        public Vector3[] FactionColours { get; set; } = [new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1)];

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
        }

        public ICapability Clone()
        {
            return new TintCapability()
            {
                DiffuseTintMask = DiffuseTintMask,
                DiffuseTintColour = DiffuseTintColour,
                DiffuseTintVariation = DiffuseTintVariation,
                UseFactionColours = UseFactionColours,
                Faction3Mask = Faction3Mask,
                Faction1_TintVariation = Faction1_TintVariation,
                Faction2_TintVariation = Faction2_TintVariation,
                Faction3_TintVariation = Faction3_TintVariation,
                FactionColours = [FactionColours[0], FactionColours[1], FactionColours[2]]
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
         
        }

        public void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
         
        }
    }
}
