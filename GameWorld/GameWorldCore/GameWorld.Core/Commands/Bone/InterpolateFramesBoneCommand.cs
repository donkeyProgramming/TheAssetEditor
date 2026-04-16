using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone
{
    public class InterpolateFramesBoneCommand : ICommand
    {
        public string HintText => "interpolates between 2 frames";

        public bool IsMutation => true;

        int _frameNr;
        int _frameANr;
        int _frameBNr;
        GameSkeleton _skeleton;
        float _interpolationValue;
        AnimationClip _animationBackup;
        AnimationClip _target;
        bool _interpolatePosition;
        bool _interpolateRotation;
        bool _interpolateScale;


        public void Configure(AnimationClip animation, int frameNrToInterpolate, int frameANr,
            int frameBNr, GameSkeleton skeleton, float interpolationValue,
            bool interpolatePosition = true, bool interpolateRotation = true, bool interpolateScale = true)
        {
            _frameNr = frameNrToInterpolate;
            _frameANr = frameANr;
            _frameBNr = frameBNr;
            _skeleton = skeleton;
            _interpolationValue = interpolationValue;
            _animationBackup = animation.Clone();
            _target = animation;
            _interpolatePosition = interpolatePosition;
            _interpolateRotation = interpolateRotation;
            _interpolateScale = interpolateScale;
        }

        public void Execute()
        {
            var sampledFrame = AnimationSampler.SampleBetween2Frames(_frameANr, _frameBNr, _interpolationValue, _skeleton, _animationBackup);
            for (var i = 0; i < sampledFrame.BoneTransforms.Count; i++)
            {
                if (_interpolatePosition) _target.DynamicFrames[_frameNr].Position[i] = sampledFrame.BoneTransforms[i].Translation;
                if (_interpolateRotation) _target.DynamicFrames[_frameNr].Rotation[i] = sampledFrame.BoneTransforms[i].Rotation;
                if (_interpolateScale) _target.DynamicFrames[_frameNr].Scale[i] = sampledFrame.BoneTransforms[i].Scale;

            }

        }

        public void Undo()
        {
            _target.DynamicFrames[_frameNr] = _animationBackup.DynamicFrames[_frameNr].Clone();
        }
    }
}
