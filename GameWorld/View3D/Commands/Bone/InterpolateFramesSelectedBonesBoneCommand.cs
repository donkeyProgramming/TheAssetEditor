using System.Collections.Generic;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone
{
    public class InterpolateFramesSelectedBonesBoneCommand : ICommand
    {
        public string HintText => "interpolates between 2 frames on selected bones";

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
        List<int> _selectedBones;

        public void Configure(AnimationClip animation, int frameNrToInterpolate, int frameANr,
            int frameBNr, GameSkeleton skeleton, float interpolationValue, List<int> selectedBones,
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
            _selectedBones = selectedBones;
        }

        public void Execute()
        {
            var sampledFrame = AnimationSampler.SampleBetween2Frames(_frameANr, _frameBNr, _interpolationValue, _skeleton, _animationBackup);
            foreach (var bone in _selectedBones)
            {
                if (_interpolatePosition) _target.DynamicFrames[_frameNr].Position[bone] = sampledFrame.BoneTransforms[bone].Translation;
                if (_interpolateRotation) _target.DynamicFrames[_frameNr].Rotation[bone] = sampledFrame.BoneTransforms[bone].Rotation;
                if (_interpolateScale) _target.DynamicFrames[_frameNr].Scale[bone] = sampledFrame.BoneTransforms[bone].Scale;
            }
        }

        public void Undo()
        {
            _target.DynamicFrames[_frameNr] = _animationBackup.DynamicFrames[_frameNr].Clone();
        }
    }
}
