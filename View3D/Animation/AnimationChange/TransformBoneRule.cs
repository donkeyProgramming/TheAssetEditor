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

        public override void TransformBone(AnimationFrame frame, int boneId, float v)
        {
            if (boneId != _metadata.TargetNode)
                return;

            var quat = new Quaternion(_metadata.Orientation);
            Matrix m = Matrix.CreateFromQuaternion(quat) * Matrix.CreateTranslation(_metadata.Position) * frame.BoneTransforms[_metadata.TargetNode].WorldTransform;
            frame.BoneTransforms[_metadata.TargetNode].WorldTransform = m;
        }
    }
}
