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
        IAnimationProvider _gameSkeleton;
        int _boneIndex;

        public SkeletonBoneAnimationResolver(IAnimationProvider gameSkeleton, int boneIndex)
        {
            _gameSkeleton = gameSkeleton;
            _boneIndex = boneIndex;
        }

        public Matrix GetWorldTransform()
        {
            return _gameSkeleton.Skeleton.GetAnimatedWorldTranform(_boneIndex);
        }
    }
}
