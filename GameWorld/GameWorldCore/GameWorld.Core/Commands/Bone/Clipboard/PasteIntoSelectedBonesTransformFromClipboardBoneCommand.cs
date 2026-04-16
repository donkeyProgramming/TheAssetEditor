// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone.Clipboard
{
    public class PasteIntoSelectedBonesTransformFromClipboardBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        List<int> _selectedBones;
        bool _pastePosition = false;
        bool _pasteRotation = false;
        bool _pasteScale = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, List<int> selectedBones = null,
            bool pastePosition = false, bool pasteRotation = false, bool pasteScale = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _selectedBones = selectedBones;
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
            if (_selectedBones == null) return;

            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count < _fromFrame.Frames.Count;

            if (isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = _fromFrame.Frames.Count - _backupFrames.Count;

                for (var i = 0; i < delta; i++)
                {
                    _animation.DynamicFrames.Add(lastFrame.Clone());
                }
            }

            var startingFrame = _fromFrame.Frames.First().Key;
            foreach (var frame in _fromFrame.Frames)
            {
                foreach (var bone in _selectedBones)
                {
                    var boneId = _intoSkeleton.GetBoneNameByIndex(bone);
                    if (boneId == "") continue;
                    if (!frame.Value.BoneIdToPosition.ContainsKey(boneId)) continue;
                    if (_pastePosition) _animation.DynamicFrames[frame.Key - startingFrame].Position[bone] = _fromFrame.Frames[frame.Key].BoneIdToPosition[boneId];
                    if (_pasteRotation) _animation.DynamicFrames[frame.Key - startingFrame].Rotation[bone] = _fromFrame.Frames[frame.Key].BoneIdToQuaternion[boneId];
                    if (_pasteScale) _animation.DynamicFrames[frame.Key - startingFrame].Scale[bone] = _fromFrame.Frames[frame.Key].BoneIdToScale[boneId];
                }
            }
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
