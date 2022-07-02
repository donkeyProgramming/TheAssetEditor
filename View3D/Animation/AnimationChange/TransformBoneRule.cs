using CommonControls.Common;
using CommonControls.FileTypes.MetaData.Definitions;
using Microsoft.Xna.Framework;
using Serilog;
using System;

namespace View3D.Animation.AnimationChange
{
    public class TransformBoneRule : AnimationChangeRule
    {
        ILogger _logger = Logging.Create<CopyRootTransform>();
        bool _hasError = false;
        Transform_v10 _metadata;

        public TransformBoneRule(Transform_v10 metadata)
        {
            _metadata = metadata;
        }

        public override void TransformBone(AnimationFrame frame, int boneId, float v)
        {
            if (boneId != _metadata.TargetNode || _hasError)
                return;

            try
            {
                var quat = new Quaternion(_metadata.Orientation);
                Matrix m = Matrix.CreateFromQuaternion(quat) * Matrix.CreateTranslation(_metadata.Position) * frame.BoneTransforms[_metadata.TargetNode].WorldTransform;
                frame.BoneTransforms[_metadata.TargetNode].WorldTransform = m;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error in {nameof(TransformBoneRule)} - {e.Message}");
                _hasError = true;
            }
        }
    }
}
