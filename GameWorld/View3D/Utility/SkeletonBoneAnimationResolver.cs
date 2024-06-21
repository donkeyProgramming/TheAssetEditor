using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Utility
{
    public class SkeletonBoneAnimationResolver
    {
        private readonly ISkeletonProvider _animationProvider;
        private readonly int _boneIndex;

        public SkeletonBoneAnimationResolver(ISkeletonProvider gameSkeleton, int boneIndex)
        {
            _animationProvider = gameSkeleton;
            _boneIndex = boneIndex;
        }

        public Matrix GetWorldTransform()
        {
            return _animationProvider.Skeleton.GetAnimatedWorldTranform(_boneIndex);
        }

        public Matrix GetWorldTransformIfAnimating()
        {
            if (_animationProvider.Skeleton != null && _animationProvider.Skeleton.AnimationPlayer.IsEnabled && _animationProvider.Skeleton.AnimationPlayer.IsPlaying && _boneIndex != -1)
                return _animationProvider.Skeleton.GetAnimatedWorldTranform(_boneIndex);
            return Matrix.Identity;
        }

        public Matrix GetTransformIfAnimating()
        {
            if (_animationProvider.Skeleton != null && _animationProvider.Skeleton.AnimationPlayer.IsEnabled && _animationProvider.Skeleton.AnimationPlayer.IsPlaying && _boneIndex != -1)
                return _animationProvider.Skeleton.GetAnimatedTranform(_boneIndex);
            return Matrix.Identity;
        }
    }
}
