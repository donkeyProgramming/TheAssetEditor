// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using View3D.Animation;

namespace View3D.Commands.Bone
{
    public class PasteTransformFromClipboardBoneCommand : ICommand
    {
        public class BoneTransformClipboardData
        {
            [JsonInclude]
            public string SkeletonName;
            [JsonInclude]
            public Dictionary<int, Frame> Frames = new ();
            public struct Frame
            {
                [JsonInclude]
                public Dictionary<string, Vector3> BoneIdToPosition = new();
                [JsonInclude]
                public Dictionary<string, Quaternion> BoneIdToQuaternion = new();
                [JsonInclude]
                public Dictionary<string, Vector3> BoneIdToScale = new();
                public Frame () { }
            }
        }

        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        List<AnimationClip.KeyFrame> _backupFrames = new();
        int _beginFrame;
        int _endFrame;
        List<int> _selectedBones;
        bool _insertExcessFrames = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, int beginFrame, int endFrame, List<int> selectedBones = null, bool insertExcessFrame = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _beginFrame = beginFrame;
            _endFrame = endFrame;
            _selectedBones = selectedBones;
            _insertExcessFrames = insertExcessFrame;
        }

        public void PasteWholeFrame()
        {
            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }

            foreach (var frame in _fromFrame.Frames)
            {
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToPosition)
                {
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key].Position[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToQuaternion)
                {
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key].Rotation[boneId] = bone.Value;
                }
                foreach (var bone in _fromFrame.Frames[frame.Key].BoneIdToScale)
                {
                    var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                    if (boneId != -1) _animation.DynamicFrames[frame.Key].Scale[boneId] = bone.Value;
                }
            }            
        }

        public void PasteIntoSelectedBones()
        {
            if (_selectedBones == null) return;
            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }


            foreach (var frame in _fromFrame.Frames)
            {
                foreach (var bone in _selectedBones)
                {
                    var boneId = _intoSkeleton.GetBoneNameByIndex(bone);
                    if (boneId == "") continue;
                    if (!frame.Value.BoneIdToPosition.ContainsKey(boneId)) continue;
                    _animation.DynamicFrames[frame.Key].Position[bone] = _fromFrame.Frames[frame.Key].BoneIdToPosition[boneId];
                    _animation.DynamicFrames[frame.Key].Rotation[bone] = _fromFrame.Frames[frame.Key].BoneIdToQuaternion[boneId];
                    _animation.DynamicFrames[frame.Key].Scale[bone] = _fromFrame.Frames[frame.Key].BoneIdToScale[boneId];
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
