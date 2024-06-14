// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Commands.Bone.Clipboard
{
    public class BoneTransformClipboardData
    {
        [JsonInclude]
        public string SkeletonName;
        [JsonInclude]
        public Dictionary<int, Frame> Frames = new();
        public struct Frame
        {
            [JsonInclude]
            public Dictionary<string, Vector3> BoneIdToPosition = new();
            [JsonInclude]
            public Dictionary<string, Quaternion> BoneIdToQuaternion = new();
            [JsonInclude]
            public Dictionary<string, Vector3> BoneIdToScale = new();
            public Frame() { }
        }
    }

    public class PasteWholeTransformFromClipboardBoneCommand : ICommand
    {
        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        bool _pastePosition = false;
        bool _pasteRotation = false;
        bool _pasteScale = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation,
            bool pastePosition = false, bool pasteRotation = false, bool pasteScale = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
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
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToPosition)
                {
                    if (!_pastePosition) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key - startingFrame].Position[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToQuaternion)
                {
                    if (!_pasteRotation) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key - startingFrame].Rotation[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToScale)
                {
                    if (!_pasteScale) continue;
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key - startingFrame].Scale[boneId] = bone.Value;
                }
            }
        }
        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
