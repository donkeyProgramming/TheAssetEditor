using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.SceneNodes;

namespace View3D.Utility
{
    public class SkeletonBoneAnimationResolver
    {
        IAnimationProvider _animationProvider;
        int _boneIndex;

        public SkeletonBoneAnimationResolver(IAnimationProvider gameSkeleton, int boneIndex)
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
            if(_animationProvider.Skeleton.AnimationPlayer.IsEnabled && _animationProvider.Skeleton.AnimationPlayer.IsPlaying)
                return _animationProvider.Skeleton.GetAnimatedWorldTranform(_boneIndex);
            return Matrix.Identity;
        }
    }
}
