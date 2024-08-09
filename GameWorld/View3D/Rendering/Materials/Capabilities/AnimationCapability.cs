using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
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

        public void SerializeToWsModel(MaterialToWsModelSerializer templateHandler)
        {
            throw new System.NotImplementedException();
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            throw new System.NotImplementedException();
        }
    }
}
