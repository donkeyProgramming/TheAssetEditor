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

        public override void ApplyBeforeWorldTransform(AnimationFrame frame)
        {
            var transform = _skeletonProvider.Skeleton.GetAnimatedWorldTranform(_boneId);
            transform.Decompose(out var scale, out var rot, out var trans);
            frame.BoneTransforms[0].Rotation =  rot * _offsetRot;// *  rot * _offsetRot; ;
            frame.BoneTransforms[0].Translation = trans;// + new Vector3(_offsetPos.Z, _offsetPos.X, _offsetPos.Y);
        }
    }
}
