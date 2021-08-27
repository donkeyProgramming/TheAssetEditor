using Microsoft.Xna.Framework;
using View3D.SceneNodes;

namespace View3D.Animation.AnimationChange
{
    public class CopyRootTransform : AnimationChangeRule
    {
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

        public override void ApplyRule(AnimationFrame frame, int boneId, float v)       
        {
          
            if (boneId != 0)
                return;

            var transform = _skeletonProvider.Skeleton.GetAnimatedWorldTranform(_boneId);
            Matrix m = Matrix.CreateFromQuaternion(_offsetRot) * Matrix.CreateTranslation(_offsetPos) * transform;
            frame.BoneTransforms[0].WorldTransform = m;
        }
    }
}
