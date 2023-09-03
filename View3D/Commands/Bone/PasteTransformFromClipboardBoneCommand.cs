// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
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
        int _pasteInTargetAnimationAtFrame;
        int _copyFramesLength;
        List<int> _selectedBones;
        bool _insertExcessFrames = false;
        bool _pastePosition = false;
        bool _pasteRotation = false;
        bool _pasteScale = false;

        public void Configure(GameSkeleton intoSkeleton, BoneTransformClipboardData copyFromFrameInClipboard, AnimationClip animation, int pasteInTargetAnimationAtFrame = 0, int copyFramesLength = 0, List<int> selectedBones = null, bool insertExcessFrame = false, 
            bool pastePosition = false, bool pasteRotation = false, bool pasteScale = false)
        {
            _intoSkeleton = intoSkeleton;
            _fromFrame = copyFromFrameInClipboard;
            _animation = animation;
            _pasteInTargetAnimationAtFrame = pasteInTargetAnimationAtFrame;
            _copyFramesLength = copyFramesLength;
            _selectedBones = selectedBones;
            _insertExcessFrames = insertExcessFrame;
            _pastePosition = pastePosition;
            _pasteRotation = pasteRotation;
            _pasteScale = pasteScale;


            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }
        }

        public void PasteWholeFrames()
        {
            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count < _fromFrame.Frames.Count;

            if(isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = _fromFrame.Frames.Count - _backupFrames.Count;

                for (int i = 0; i < delta; i++)
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

        public void PasteInRanges()
        {
            int frameNr = _pasteInTargetAnimationAtFrame;
            var copyFramesEnds = _copyFramesLength + frameNr;
            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count < copyFramesEnds;

            if (isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = _copyFramesLength - _backupFrames.Count;

                for (int i = 0; i < delta; i++)
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
                    if (boneId != -1) _animation.DynamicFrames[frameNr ].Scale[boneId] = bone.Value;
                }
                frameNr++;
            }
        }

        public void PasteIntoSelectedBones()
        {
            if (_selectedBones == null) return;

            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count < _fromFrame.Frames.Count;

            if (isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = _fromFrame.Frames.Count - _backupFrames.Count;

                for (int i = 0; i < delta; i++)
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

        public void PasteIntoSelectedBonesInRanges()
        {
            int frameNr = _pasteInTargetAnimationAtFrame;
            var copyFramesEnds = _copyFramesLength + frameNr - 1;
            var isTheTargetFramesShorterThanCopiedFrames = _backupFrames.Count <  _copyFramesLength;

            if (isTheTargetFramesShorterThanCopiedFrames)
            {
                var lastFrame = _animation.DynamicFrames.Last().Clone();
                var delta = copyFramesEnds - _backupFrames.Count;

                for (int i = 0; i < delta; i++)
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
                    if (_pasteRotation)_animation.DynamicFrames[frameNr].Rotation[bone] = _fromFrame.Frames[frame.Key].BoneIdToQuaternion[boneId];
                    if (_pasteScale) _animation.DynamicFrames[frameNr].Scale[bone] = _fromFrame.Frames[frame.Key].BoneIdToScale[boneId];
                }

                frameNr++;
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
