using Microsoft.Xna.Framework;
using View3D.SceneNodes;

namespace View3D.Utility
{
    public class SkeletonBoneAnimationResolver
    {
        ISkeletonProvider _animationProvider;
        int _boneIndex;

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
            if(_animationProvider.Skeleton != null && _animationProvider.Skeleton.AnimationPlayer.IsEnabled && _animationProvider.Skeleton.AnimationPlayer.IsPlaying && _boneIndex != -1)
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
