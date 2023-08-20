// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using View3D.Animation;

namespace View3D.Commands.Bone
{
    public class PasteTransformBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform";

        public bool IsMutation => true;

        AnimationClip.KeyFrame _fromFrame;
        AnimationClip _animation;
        AnimationClip.KeyFrame _backupFrame;
        int _targetFrame;
        List<int> _selectedBones;

        public void Configure(AnimationClip.KeyFrame copyFromFrame, AnimationClip animation, int targetFrame, List<int> selectedBones = null)
        {
            _fromFrame = copyFromFrame;
            _animation = animation;
            _targetFrame = targetFrame; 
            _selectedBones = selectedBones;
        }

        public void PasteWholeFrame()
        {
            _backupFrame = _animation.DynamicFrames[_targetFrame].Clone();
            _animation.DynamicFrames[_targetFrame] = _fromFrame.Clone();
        }

        public void PasteIntoSelectedBones()
        {
            if (_selectedBones == null) return;

            foreach (var bone in _selectedBones)
            {
                _animation.DynamicFrames[_targetFrame].Position[bone] = _fromFrame.Position[bone];
                _animation.DynamicFrames[_targetFrame].Rotation[bone] = _fromFrame.Rotation[bone];
                _animation.DynamicFrames[_targetFrame].Scale[bone] = _fromFrame.Scale[bone];
            }
        }

        public void Execute()
        {
            
        }
        public void Undo()
        {
            _animation.DynamicFrames[_targetFrame] = _backupFrame.Clone();
        }
    }
}
