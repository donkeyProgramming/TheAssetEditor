// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone.Clipboard
{
    public class PasteIntoSelectedBonesInRangeTransformFromClipboardBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _pasteInTargetAnimationAtFrame;
        int _copyFramesLength;
        List<int> _selectedBones;
        bool _pastePosition = false;
        bool _pasteRotation = false;
        bool _pasteScale = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, int pasteInTargetAnimationAtFrame = 0, int copyFramesLength = 0, List<int> selectedBones = null,
            bool pastePosition = false, bool pasteRotation = false, bool pasteScale = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _pasteInTargetAnimationAtFrame = pasteInTargetAnimationAtFrame;
            _copyFramesLength = copyFramesLength;
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

                foreach (var bone in _selectedBones)
                {
                    var boneId = _intoSkeleton.GetBoneNameByIndex(bone);
                    if (boneId == "") continue;
                    if (!frame.Value.BoneIdToPosition.ContainsKey(boneId)) continue;
                    if (_pastePosition) _animation.DynamicFrames[frameNr].Position[bone] = _fromFrame.Frames[frame.Key].BoneIdToPosition[boneId];
                    if (_pasteRotation) _animation.DynamicFrames[frameNr].Rotation[bone] = _fromFrame.Frames[frame.Key].BoneIdToQuaternion[boneId];
                    if (_pasteScale) _animation.DynamicFrames[frameNr].Scale[bone] = _fromFrame.Frames[frame.Key].BoneIdToScale[boneId];
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
