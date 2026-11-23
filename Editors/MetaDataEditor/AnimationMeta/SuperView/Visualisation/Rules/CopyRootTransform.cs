using GameWorld.Core.Animation;
using GameWorld.Core.Animation.AnimationChange;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace Editors.AnimationMeta.SuperView.Visualisation.Rules
{
    public class CopyRootTransform : ILocalSpaceAnimationRule
    {
        readonly ILogger _logger = Logging.Create<CopyRootTransform>();
        readonly ISkeletonProvider _skeletonProvider;
        readonly int _boneId;

        bool _hasError = false;
        // Read on every frame, so the prop follows its tag while the tag is being dragged.
        readonly Func<Vector3> _offsetPos;
        readonly Func<Quaternion> _offsetRot;

        public CopyRootTransform(ISkeletonProvider skeleton, int boneId, Func<Vector3> offsetPos, Func<Quaternion> offsetRot)
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
                var m = Matrix.CreateFromQuaternion(_offsetRot()) * Matrix.CreateTranslation(_offsetPos()) * transform;
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
