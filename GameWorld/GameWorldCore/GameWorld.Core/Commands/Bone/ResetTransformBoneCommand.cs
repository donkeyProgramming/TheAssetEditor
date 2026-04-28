// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone
{
    public class ResetTransformBoneCommand : ICommand
    {
        AnimationClip.KeyFrame _oldFrame;
        AnimationClip _originalAnimationClip;
        AnimationClip _currentAnimationClip;
        int _currentFrame;


        public string HintText => "Bone Reset Transform";

        public bool IsMutation => true;

        public void Configure(AnimationClip currentAnimation, AnimationClip originalAnimation, int currentFrame)
        {
            _currentFrame = currentFrame;
            _originalAnimationClip = originalAnimation;
            _currentAnimationClip = currentAnimation;
            _oldFrame = _currentAnimationClip.DynamicFrames[_currentFrame].Clone();

        }

        public void Execute()
        {
            _currentAnimationClip.DynamicFrames[_currentFrame] = _originalAnimationClip.DynamicFrames[_currentFrame].Clone();
        }

        public void Undo()
        {
            _currentAnimationClip.DynamicFrames[_currentFrame] = _oldFrame.Clone();
        }
    }
}
