using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
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
        public bool UseTinting { get; set; } = false;
        public Vector4 Faction3Mask { get; set; } = Vector4.Zero;
        public float Faction1_TintVariation { get; set; } = 0;  //Replace as vector3?
        public float Faction2_TintVariation { get; set; } = 0;
        public float Faction3_TintVariation { get; set; } = 0;
        public Vector3[] FactionColours { get; set; } = [new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1)];
        public Vector3[] TintColours { get; set; } = [new Vector3(0.7f, 0.7f, 0.6f), new Vector3(0.6f, 0.6f, 0.7f), new Vector3(0.7f, 0.6f, 0.6f)];

        public void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {
            effect.GetParameter("CapabilityFlag_ApplyTinting").SetValue(ApplyCapability);

            effect.GetParameter("Tint_UseFactionColours").SetValue(UseFactionColours);
            effect.GetParameter("Tint_FactionsColours").SetValue(FactionColours);
                                 
            effect.GetParameter("Tint_UseTinting").SetValue(UseTinting);
            effect.GetParameter("Tint_TintColours").SetValue(TintColours);
            effect.GetParameter("Tint_TintColours").SetValue(TintColours);
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

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            var typedCap = otherCap as TintCapability;
            if (typedCap == null)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");
            return (true, "");
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
