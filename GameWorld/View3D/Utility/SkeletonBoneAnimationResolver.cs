using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using System;

namespace GameWorld.Core.Utility
{
    public class SkeletonBoneAnimationResolver
    {
        private readonly ISkeletonProvider _animationProvider;
        private int _boneIndex = -1;
        private readonly string _boneName;

        public SkeletonBoneAnimationResolver(ISkeletonProvider gameSkeleton, int boneIndex)
        {
            _animationProvider = gameSkeleton;
            _boneIndex = boneIndex;
            if (_boneIndex != -1 && _animationProvider.Skeleton != null)
                _boneName = _animationProvider.Skeleton.GetBoneNameByIndex(_boneIndex);
        }

        public SkeletonBoneAnimationResolver(ISkeletonProvider gameSkeleton, string boneName)
        {
            _animationProvider = gameSkeleton;
            _boneName = boneName;
        }

        private void TryResolveBoneIndex()
        {
            if (_boneIndex == -1 && !string.IsNullOrWhiteSpace(_boneName) && _animationProvider.Skeleton != null)
            {
                _boneIndex = _animationProvider.Skeleton.GetBoneIndexByName(_boneName);
            }
        }

        public Matrix GetWorldTransform()
        {
            TryResolveBoneIndex();
            if (_boneIndex != -1 && _animationProvider.Skeleton != null)
                return _animationProvider.Skeleton.GetAnimatedWorldTranform(_boneIndex);
            return Matrix.Identity;
        }

        public Matrix GetWorldTransformIfAnimating()
        {
            TryResolveBoneIndex();
            //移除了对 AnimationPlayer.IsPlaying 的限制
            if (_animationProvider.Skeleton != null && _boneIndex != -1)
                return _animationProvider.Skeleton.GetAnimatedWorldTranform(_boneIndex);

            return Matrix.Identity;
        }

        public Matrix GetTransformIfAnimating()
        {
            TryResolveBoneIndex();
            //移除了对 AnimationPlayer.IsPlaying 的限制
            if (_animationProvider.Skeleton != null && _boneIndex != -1)
                return _animationProvider.Skeleton.GetAnimatedTranform(_boneIndex);

            return Matrix.Identity;
        }
    }
}
