// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using View3D.Animation;

namespace View3D.Commands.Bone
{
    public class PasteWholeTransformBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform";

        public bool IsMutation => true;

        AnimationClip.KeyFrame _fromFrame;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _startingFrame;
        int _endFrame;
        bool _pastePosition = true;
        bool _pasteRotation = true;
        bool _pasteScale = true;


        public void Configure(AnimationClip.KeyFrame copyFromFrame, AnimationClip animation, int startingFrame, int endFrame, 
            bool pastePosition = true, bool pasteRotation = true, bool pasteScale = true)
        {
            _fromFrame = copyFromFrame;
            _animation = animation;
            _startingFrame = startingFrame;
            _endFrame = endFrame;
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
            for (int frameNr = _startingFrame; frameNr <= _endFrame; frameNr++)
            {
                var clone = _fromFrame.Clone();
                if (_pastePosition) _animation.DynamicFrames[frameNr].Position = clone.Position;
                if (_pasteRotation) _animation.DynamicFrames[frameNr].Rotation = clone.Rotation;
                if (_pasteScale) _animation.DynamicFrames[frameNr].Scale = clone.Scale;
            }
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
