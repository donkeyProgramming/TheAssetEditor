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
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _startingFrame;
        int _endFrame;
        List<int> _selectedBones;
        bool _pastePosition = true;
        bool _pasteRotation = true;
        bool _pasteScale = true;


        public void Configure(AnimationClip.KeyFrame copyFromFrame, AnimationClip animation, int startingFrame, int endFrame, List<int> selectedBones = null, 
            bool pastePosition = true, bool pasteRotation = true, bool pasteScale = true)
        {
            _fromFrame = copyFromFrame;
            _animation = animation;
            _startingFrame = startingFrame;
            _endFrame = endFrame;
            _selectedBones = selectedBones;
            _pastePosition = pastePosition;
            _pasteRotation = pasteRotation;
            _pasteScale = pasteScale;

            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }
        }

        public void PasteWholeFrame()
        {
            for (int frameNr = _startingFrame; frameNr <= _endFrame; frameNr++)
            {
                var clone = _backupFrames[frameNr].Clone();
                if (_pastePosition) _animation.DynamicFrames[frameNr].Position = clone.Position;
                if (_pasteRotation) _animation.DynamicFrames[frameNr].Rotation = clone.Rotation;
                if (_pasteScale) _animation.DynamicFrames[frameNr].Scale = clone.Scale;
            }
        }

        public void PasteIntoSelectedBones()
        {
            if (_selectedBones == null) return;

            foreach (var bone in _selectedBones)
            {
                for (int frameNr = _startingFrame; frameNr <= _endFrame; frameNr++)
                {
                    if (_pastePosition) _animation.DynamicFrames[frameNr].Position[bone] = _fromFrame.Position[bone];
                    if (_pasteRotation) _animation.DynamicFrames[frameNr].Rotation[bone] = _fromFrame.Rotation[bone];
                    if (_pasteScale) _animation.DynamicFrames[frameNr].Scale[bone] = _fromFrame.Scale[bone];
                }
            }
        }

        public void Execute()
        {
            
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
