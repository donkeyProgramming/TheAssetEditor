// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public void Configure(AnimationClip.KeyFrame copyFromFrame, AnimationClip animation, int targetFrame)
        {
            _fromFrame = copyFromFrame;
            _animation = animation;
            _targetFrame = targetFrame; 
        }

        public void PasteWholeFrame()
        {
            _backupFrame = _animation.DynamicFrames[_targetFrame].Clone();
            _animation.DynamicFrames[_targetFrame] = _fromFrame.Clone();
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
