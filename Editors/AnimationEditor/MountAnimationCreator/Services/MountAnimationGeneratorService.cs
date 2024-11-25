using AnimationEditor.MountAnimationCreator.ViewModels;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnimationEditor.MountAnimationCreator.Services
{
    class MountAnimationGeneratorService
    {
        AnimationSettingsViewModel _animationSettings;
        int _mountVertexId;
        int _riderBoneIndex;
        GameSkeleton _riderSkeleton;
        GameSkeleton _mountSkeleton;

        MeshAnimationHelper _mountVertexPositionResolver;
        public MountAnimationGeneratorService(AnimationSettingsViewModel animationSettings, Rmv2MeshNode mountMesh, int mountVertexId, int riderBoneIndex, SceneObject rider, SceneObject mount)
        {
            _animationSettings = animationSettings;
            _mountVertexId = mountVertexId;

            _riderBoneIndex = riderBoneIndex;
            _riderSkeleton = rider.Skeleton;
            _mountSkeleton = mount.Skeleton;

            float mountScale = (float)_animationSettings.Scale.Value;
            mount.SetTransform(Matrix.CreateScale(mountScale));
            _mountVertexPositionResolver = new MeshAnimationHelper(mountMesh, Matrix.CreateScale(mountScale));
        }

        public AnimationClip GenerateMountAnimation(AnimationClip mountAnimation, AnimationClip riderAnimation)
        {
            Vector3 translationOffset = new Vector3((float)_animationSettings.Translation.X.Value, (float)_animationSettings.Translation.Y.Value, (float)_animationSettings.Translation.Z.Value);
            Vector3 rotationOffsetVector = new Vector3((float)_animationSettings.Rotation.X.Value, (float)_animationSettings.Rotation.Y.Value, (float)_animationSettings.Rotation.Z.Value);
            var rotationOffset = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotationOffsetVector.X), MathHelper.ToRadians(rotationOffsetVector.Y), MathHelper.ToRadians(rotationOffsetVector.Z));

            var newRiderAnim = riderAnimation.Clone();

            GameWorld.Core.Animation.AnimationEditor.LoopAnimation(newRiderAnim, (int)_animationSettings.LoopCounter.Value);

            // Resample
            if (_animationSettings.FitAnimation)
                newRiderAnim = GameWorld.Core.Animation.AnimationEditor.ReSample(_riderSkeleton, newRiderAnim, mountAnimation.DynamicFrames.Count, mountAnimation.PlayTimeInSec);

            var maxFrameCount = Math.Min(mountAnimation.DynamicFrames.Count, newRiderAnim.DynamicFrames.Count);
            for (int i = 0; i < maxFrameCount; i++)
            {
                var mountFrame = AnimationSampler.Sample(i, 0, _mountSkeleton, mountAnimation);
                var mountBoneWorldMatrix = _mountVertexPositionResolver.GetVertexTransformWorld(mountFrame, _mountVertexId);
                mountBoneWorldMatrix.Decompose(out var _, out var mountVertexRot, out var mountVertexPos);

                // Make sure the rider moves along in the world with the same speed as the mount when there is a root bone
                if (_animationSettings.IsRootNodeAnimation)
                {
                    newRiderAnim.DynamicFrames[i].Position[0] = mountFrame.BoneTransforms[0].Translation;
                    newRiderAnim.DynamicFrames[i].Rotation[0] = Quaternion.Identity;
                }

                var origianlRotation = Quaternion.Identity;
                if (_animationSettings.KeepRiderRotation)
                {
                    var riderFrame = AnimationSampler.Sample(i, 0, _riderSkeleton, newRiderAnim);
                    var riderBoneWorldmatrix = riderFrame.GetSkeletonAnimatedWorld(_riderSkeleton, _riderBoneIndex);
                    riderBoneWorldmatrix.Decompose(out var _, out origianlRotation, out var _);
                }

                var originalPosition = newRiderAnim.DynamicFrames[i].Position[_riderBoneIndex];
                var originalRotation = newRiderAnim.DynamicFrames[i].Rotation[_riderBoneIndex];

                var newRiderPosition = mountVertexPos + translationOffset;
                if (_animationSettings.IsRootNodeAnimation)
                    newRiderPosition = newRiderPosition - mountFrame.BoneTransforms[0].Translation;
                var newRiderRotation = Quaternion.Multiply(Quaternion.Multiply(mountVertexRot, origianlRotation), rotationOffset);

                var riderPositionDiff = newRiderPosition - originalPosition;
                var riderRotationDiff = newRiderRotation * Quaternion.Inverse(originalRotation);

                newRiderAnim.DynamicFrames[i].Position[_riderBoneIndex] = newRiderPosition;
                newRiderAnim.DynamicFrames[i].Rotation[_riderBoneIndex] = newRiderRotation;

                // Process attachment/prop points
                List<int> propBones = new List<int>();
                if (_animationSettings.IsRootNodeAnimation)
                {
                    var parentBoneIndex = _riderSkeleton.GetParentBoneIndex(_riderBoneIndex);
                    if (parentBoneIndex != -1)
                        propBones = _riderSkeleton.GetDirectChildBones(parentBoneIndex);
                }
                else
                {
                    propBones = _riderSkeleton.GetDirectChildBones(-1);
                }

                foreach (var propBoneId in propBones)
                {
                    if (propBoneId == _riderBoneIndex)
                        continue;
                    newRiderAnim.DynamicFrames[i].Position[propBoneId] += riderPositionDiff;
                    newRiderAnim.DynamicFrames[i].Rotation[propBoneId] = riderRotationDiff * newRiderAnim.DynamicFrames[i].Rotation[propBoneId];
                }
            }

            return newRiderAnim;
        }


        internal GameSkeleton GetRiderSkeleton()
        {
            return _riderSkeleton;
        }

        //static public PackFile SaveAnimation(PackFileService pfs, string riderAnimationName, string savePrefix, bool ensureUniqeName, AnimationClip clip, GameSkeleton skeleton)
        //{
        //    var animFile = clip.ConvertToFileFormat(skeleton);
        //    var bytes = AnimationFile.ConvertToBytes(animFile);
        //
        //    string savePath = "";
        //    if (string.IsNullOrWhiteSpace(savePrefix) == false)
        //    {
        //        if (ensureUniqeName)
        //            savePath = GenerateNewAnimationName(pfs, riderAnimationName, savePrefix);
        //        else
        //            savePath = Path.GetDirectoryName(riderAnimationName) + "\\" + savePrefix + Path.GetFileName(riderAnimationName);
        //    }
        //
        //    return SaveHelper.Save(pfs, savePath, null, bytes);
        //}

        static string GenerateNewAnimationName(IPackFileService pfs, string fullPath, string prefix, int numberId = 0)
        {
            string numberPostFix = "";
            if (numberId != 0)
                numberPostFix = "_" + numberId;

            var potentialName = Path.GetDirectoryName(fullPath) + "\\" + prefix + numberPostFix + Path.GetFileName(fullPath);
            var fileRef = pfs.FindFile(potentialName);
            if (fileRef == null)
                return potentialName;
            else
                return GenerateNewAnimationName(pfs, fullPath, prefix, numberId + 1);
        }

        static public bool IsCopyOnlyAnimation(string riderSlot)
        {
            string[] startWidth = new string[] {
                "HAND_POSE_",
                "DOCK_",
                "PERSISTENT_METADATA_",
                "PORTHOLE_",
                "RIDER_CAST_SPELL_", "RIDER_MOVING_ATTACK_", "RIDER_CELEBRATE_", "RIDER_FIRE_"};

            string[] equals = new string[] {
                "STAND",
                "MISSING_ANIM" ,
                "RIDER_SHOOT_READY",
                "RIDER_COMBAT_READY",
                "RIDER_RELOAD",
                "RIDER_STAND_TO_RIDER_COMBAT_READY",
                "RIDER_COMBAT_READY_TO_RIDER_STAND",
                "RIDER_SHOOT_READY_TO_RIDER_STAND",
                "RIDER_STAND_TO_RIDER_SHOOT_READY",
                "RIDER_SHOOT_READY_TO_RIDER_RELOAD",
                "RIDER_RELOAD_TO_RIDER_SHOOT_READY",
            };

            var startWithRes = startWidth.Any(x => riderSlot.StartsWith(x, StringComparison.CurrentCultureIgnoreCase));
            var eauqlRes = equals.Any(x => riderSlot.Equals(x, StringComparison.CurrentCultureIgnoreCase));

            return startWithRes || eauqlRes;
        }
    }
}
