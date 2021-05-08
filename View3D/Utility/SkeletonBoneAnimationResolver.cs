using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;

namespace View3D.Utility
{
    public class SkeletonBoneAnimationResolver
    {
        GameSkeleton _gameSkeleton;
        int _boneIndex;

        public SkeletonBoneAnimationResolver(GameSkeleton gameSkeleton, int boneIndex)
        {
            _gameSkeleton = gameSkeleton;
            _boneIndex = boneIndex;
        }

        public Matrix GetWorldTransform()
        {
            return _gameSkeleton.GetAnimatedWorldTranform(_boneIndex);
        }

    }
}
