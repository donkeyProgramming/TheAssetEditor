using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    public class AnimationCapability : ICapability
    {
        public bool ApplyAnimation { get; set; }
        public bool AnimationInformation { get; set; }
        public Matrix[]? AnimationTransforms { get; set; }
        public int AnimationWeightCount { get; set; }

        public void Apply(Effect effect, ResourceLibrary _)
        {
            effect.Parameters["CapabilityFlag_ApplyAnimation"].SetValue(ApplyAnimation);
            effect.Parameters["Animation_WeightCount"].SetValue(AnimationWeightCount);
            effect.Parameters["Animation_Tranforms"].SetValue(AnimationTransforms);
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
           
        }

        public ICapability Clone()
        {
            return new AnimationCapability()
            { 
                ApplyAnimation = ApplyAnimation,
                AnimationInformation = AnimationInformation,
                AnimationTransforms = AnimationTransforms,
                AnimationWeightCount = AnimationWeightCount,
            };
        }
    }
}
