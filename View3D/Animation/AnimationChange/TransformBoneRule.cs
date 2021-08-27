using Microsoft.Xna.Framework;

namespace View3D.Animation.AnimationChange
{
    public class TransformBoneRule : AnimationChangeRule
    {
        FileTypes.MetaData.Instances.Transform _metadata;

        public TransformBoneRule(FileTypes.MetaData.Instances.Transform metadata)
        {
            _metadata = metadata;
        }

        public override void ApplyBeforeWorldTransform(AnimationFrame frame)
        {
            frame.BoneTransforms[_metadata.TargetNode].Translation += _metadata.Position;
            frame.BoneTransforms[_metadata.TargetNode].Rotation = frame.BoneTransforms[_metadata.TargetNode].Rotation *  new Quaternion(_metadata.Orientation);
        }
    }
}
