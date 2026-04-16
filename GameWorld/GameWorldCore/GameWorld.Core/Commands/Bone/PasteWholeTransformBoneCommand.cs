// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone
{
    public class PasteWholeTransformBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform";

        public bool IsMutation => true;

        AnimationClip.KeyFrame _fromFrame;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _startingFrame;
        bool _pastePosition = true;
        bool _pasteRotation = true;
        bool _pasteScale = true;


        public void Configure(AnimationClip.KeyFrame copyFromFrame, AnimationClip animation, int startingFrame,
            bool pastePosition = true, bool pasteRotation = true, bool pasteScale = true)
        {
            _fromFrame = copyFromFrame;
            _animation = animation;
            _startingFrame = startingFrame;
            _pastePosition = pastePosition;
            _pasteRotation = pasteRotation;
            _pasteScale = pasteScale;

            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }
        }
        public void Execute()
        {
            var clone = _fromFrame.Clone();
            var replace = _animation.DynamicFrames[_startingFrame].Clone();
            if (_pastePosition) replace.Position = clone.Position;
            if (_pasteRotation) replace.Rotation = clone.Rotation;
            if (_pasteScale) replace.Scale = clone.Scale;

            _animation.DynamicFrames[_startingFrame] = replace;
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
