// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone.Clipboard
{
    public class PasteWholeInRangeTransformFromClipboardBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _pasteInTargetAnimationAtFrame;
        int _copyFramesLength;
        bool _pastePosition = false;
        bool _pasteRotation = false;
        bool _pasteScale = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, int pasteInTargetAnimationAtFrame = 0, int copyFramesLength = 0,
            bool pastePosition = false, bool pasteRotation = false, bool pasteScale = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _pasteInTargetAnimationAtFrame = pasteInTargetAnimationAtFrame;
            _copyFramesLength = copyFramesLength;
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
            var frameNr = _pasteInTargetAnimationAtFrame;
            var copyFramesEnds = _copyFramesLength + frameNr;
            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count < copyFramesEnds;

            if (isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = copyFramesEnds - _backupFrames.Count;

                for (var i = 0; i < delta; i++)
                {
                    _animation.DynamicFrames.Add(lastFrame.Clone());
                }
            }

            foreach (var frame in _fromFrame.Frames)
            {
                if (frameNr > copyFramesEnds) break;

                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToPosition)
                {
                    if (!_pastePosition) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frameNr].Position[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToQuaternion)
                {
                    if (!_pasteRotation) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frameNr].Rotation[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToScale)
                {
                    if (!_pasteScale) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frameNr].Scale[boneId] = bone.Value;
                }
                frameNr++;
            }
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
