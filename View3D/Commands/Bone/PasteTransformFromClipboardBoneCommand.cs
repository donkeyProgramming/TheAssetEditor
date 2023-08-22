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
            public Dictionary<string, Vector3> BoneIdToPosition = new();
            [JsonInclude]
            public Dictionary<string, Quaternion> BoneIdToQuaternion = new();
            [JsonInclude]
            public Dictionary<string, Vector3> BoneIdToScale = new();
        }

        public string HintText => "Copy/paste bone transform from clipboard";

        public bool IsMutation => true;

        BoneTransformClipboardData _fromFrame;
        GameSkeleton _intoSkeleton;
        AnimationClip _animation;
        AnimationClip.KeyFrame _backupFrame;
        int _targetFrame;
        List<int> _selectedBones;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, int targetFrame, List<int> selectedBones = null)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _targetFrame = targetFrame; 
            _selectedBones = selectedBones;
        }

        public void PasteWholeFrame()
        {
            _backupFrame = _animation.DynamicFrames[_targetFrame].Clone();
            foreach (var bone in _fromFrame.BoneIdToPosition)
            {
                var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                if (boneId != -1) _animation.DynamicFrames[_targetFrame].Position[boneId] = bone.Value;
            }
            foreach (var bone in _fromFrame.BoneIdToQuaternion)
            {
                var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                if (boneId != -1) _animation.DynamicFrames[_targetFrame].Rotation[boneId] = bone.Value;
            }
            foreach (var bone in _fromFrame.BoneIdToScale)
            {
                var boneId = _intoSkeleton.GetBoneIndexByName(bone.Key);
                if (boneId != -1) _animation.DynamicFrames[_targetFrame].Scale[boneId] = bone.Value;
            }
        }

        public void PasteIntoSelectedBones()
        {
            if (_selectedBones == null) return;

            foreach (var bone in _selectedBones)
            {
                var boneId = _intoSkeleton.GetBoneNameByIndex(bone);
                if (boneId == "") continue;
                if (!_fromFrame.BoneIdToPosition.ContainsKey(boneId)) continue;
                _animation.DynamicFrames[_targetFrame].Position[bone] = _fromFrame.BoneIdToPosition[boneId];
                _animation.DynamicFrames[_targetFrame].Rotation[bone] = _fromFrame.BoneIdToQuaternion[boneId];
                _animation.DynamicFrames[_targetFrame].Scale[bone] = _fromFrame.BoneIdToScale[boneId];
            }
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
