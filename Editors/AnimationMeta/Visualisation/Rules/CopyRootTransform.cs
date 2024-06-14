using GameWorld.Core.Animation;
using GameWorld.Core.Animation.AnimationChange;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using System;

namespace Editors.AnimationMeta.Visualisation.Rules
{
    public class CopyRootTransform : ILocalSpaceAnimationRule
    {
        ILogger _logger = Logging.Create<CopyRootTransform>();
        bool _hasError = false;

        ISkeletonProvider _skeletonProvider;
        int _boneId;
        Vector3 _offsetPos;
        Quaternion _offsetRot;

        public CopyRootTransform(ISkeletonProvider skeleton, int boneId, Vector3 offsetPos, Quaternion offsetRot)
        {
            _skeletonProvider = skeleton;
            _boneId = boneId;
            _offsetPos = offsetPos;
            _offsetRot = offsetRot;
        }

        public void TransformFrameLocalSpace(AnimationFrame frame, int boneId, float v)
        {
            if (boneId != 0 || _hasError || _boneId == -1)
                return;

            try
            {
                var transform = _skeletonProvider.Skeleton.GetAnimatedWorldTranform(_boneId);
                var m = Matrix.CreateFromQuaternion(_offsetRot) * Matrix.CreateTranslation(_offsetPos) * transform;
                frame.BoneTransforms[0].WorldTransform = m;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error in {nameof(CopyRootTransform)} - {e.Message}");
                _hasError = true;
            }
        }
    }
}
