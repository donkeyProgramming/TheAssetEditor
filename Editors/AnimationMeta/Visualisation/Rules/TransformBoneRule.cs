using System;
using GameWorld.Core.Animation;
using GameWorld.Core.Animation.AnimationChange;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.AnimationMeta.Definitions;

namespace Editors.AnimationMeta.Visualisation.Rules
{
    public class TransformBoneRule : ILocalSpaceAnimationRule
    {
        ILogger _logger = Logging.Create<CopyRootTransform>();
        bool _hasError = false;
        Transform_v10 _metadata;

        public TransformBoneRule(Transform_v10 metadata)
        {
            _metadata = metadata;
        }

        public void TransformFrameWorldSpace(AnimationFrame frame, float time) { }

        public void TransformFrameLocalSpace(AnimationFrame frame, int boneId, float time)
        {
            if (boneId != _metadata.TargetNode || _hasError)
                return;

            try
            {
                var quat = new Quaternion(_metadata.Orientation);
                var m = Matrix.CreateFromQuaternion(quat) * Matrix.CreateTranslation(_metadata.Position) * frame.BoneTransforms[_metadata.TargetNode].WorldTransform;
                frame.BoneTransforms[_metadata.TargetNode].WorldTransform = m;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error in {nameof(TransformBoneRule)} - {e.Message}");
                _hasError = true;
            }
        }
    }
}
