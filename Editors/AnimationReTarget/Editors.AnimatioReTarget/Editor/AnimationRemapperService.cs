using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Settings;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Editors.AnimatioReTarget.Editor
{
    public class AnimationRemapperService
    {
        private readonly IEnumerable<SkeletonBoneNode_new> _bones;
        private readonly AnimationGenerationSettings _settings;

        public AnimationRemapperService(AnimationGenerationSettings settings, IEnumerable<SkeletonBoneNode_new> bones)
        {
            _settings = settings;
            _bones = bones;
        }

        public AnimationClip ReMapAnimation(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToCopy)
        {
            var newFrameCount = (int)(_settings.AnimationSpeedMult * animationToCopy.DynamicFrames.Count);
            var newPlayTime = (float)_settings.AnimationSpeedMult * animationToCopy.PlayTimeInSec;

            //animationToCopy.RemoveOptimizations(copyFromSkeleton);
            var resampledAnimationToCopy = GameWorld.Core.Animation.AnimationEditor.ReSample(copyFromSkeleton, animationToCopy, newFrameCount, newPlayTime);
            var newAnimation = CreateNewAnimation(copyToSkeleton, resampledAnimationToCopy);

            if (copyFromSkeleton.SkeletonName != copyToSkeleton.SkeletonName)
                TransferAnimationWorld(copyFromSkeleton, copyToSkeleton, resampledAnimationToCopy, newAnimation);
            else
                newAnimation = resampledAnimationToCopy;

            if (_settings.ApplyRelativeScale)
                ApplyRelativeScale(copyFromSkeleton, copyToSkeleton, newAnimation);

            // Apply the "rules"
            SnapBonesToWorld(copyFromSkeleton, copyToSkeleton, newAnimation, resampledAnimationToCopy);
            FreezeBones(copyToSkeleton, newAnimation);
            ApplyOffsets(copyToSkeleton, newAnimation);
            FixAttachmentPoints(copyFromSkeleton, copyToSkeleton, newAnimation, resampledAnimationToCopy);
            ApplyAnimationScale(newAnimation, copyToSkeleton);
            ApplyBoneLengthMult(newAnimation, copyToSkeleton);

            return newAnimation;
        }


        void TransferAnimationWorld(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToCopy, AnimationClip newAnimation)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentCopyToFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, newAnimation);
                    var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);

                    var desiredBonePosWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, i);

                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (mappedIndex != null)
                    {
                        var targetBoneIndex = mappedIndex.Value;
                        desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);
                    }

                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    if (fromParentBoneIndex != -1)
                    {
                        // Convert to local space 
                        var parentWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);
                        desiredBonePosWorld = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    }

                    desiredBonePosWorld.Decompose(out var _, out var boneRotation, out var bonePosition);

                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;
                    if (boneSettings.ApplyRotation == true)
                        newAnimation.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    if (boneSettings.ApplyTranslation == true)
                        newAnimation.DynamicFrames[frameIndex].Position[i] = bonePosition;

                }
            }
        }

        void ApplyRelativeScale(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);

                    if (mappedIndex != null)
                    {
                        var targetBoneIndex = mappedIndex.Value;
                        var copyFromParentIndex = copyFromSkeleton.GetParentBoneIndex(targetBoneIndex);
                        var copyToParentIndex = copyToSkeleton.GetParentBoneIndex(i);

                        if (copyToParentIndex != -1 && copyFromParentIndex != -1)
                        {
                            var toBone0 = copyToSkeleton.GetWorldTransform(i).Translation;
                            var toBone1 = copyToSkeleton.GetWorldTransform(copyToParentIndex).Translation;
                            var targetBoneLength = Vector3.Distance(toBone0, toBone1);

                            var fromBone0 = copyFromSkeleton.GetWorldTransform(targetBoneIndex).Translation;
                            var fromBone1 = copyFromSkeleton.GetWorldTransform(copyFromParentIndex).Translation;
                            var fromBoneLength = Vector3.Distance(fromBone0, fromBone1);

                            if (fromBoneLength == 0 || targetBoneLength == 0)
                            {
                                targetBoneLength = 1;
                                fromBoneLength = 1;
                            }

                            var relativeScale = targetBoneLength / fromBoneLength;
                            animationToScale.DynamicFrames[frameIndex].Position[i] = animationToScale.DynamicFrames[frameIndex].Position[i] * relativeScale;
                        }
                    }
                }
            }
        }

        void SnapBonesToWorld(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale, AnimationClip animationToCopy)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);

                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, animationToScale);

                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;
                    if (boneSettings.ForceSnapToWorld == false)
                        continue;

                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (mappedIndex == null)
                        continue;

                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var targetBoneIndex = mappedIndex.Value;
                    var desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);

                    var parentWorld = currentFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);

                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    // Apply the values to the animation
                    animationToScale.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    animationToScale.DynamicFrames[frameIndex].Position[i] = bonePosition;
                }
            }
        }

        void ApplyOffsets(GameSkeleton copyToSkeleton, AnimationClip animationToScale)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var currentFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, animationToScale);

                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;

                    var desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value)) *
                        currentFrame.GetSkeletonAnimatedWorld(copyToSkeleton, i) *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.TranslationOffset.X.Value, (float)boneSettings.TranslationOffset.Y.Value, (float)boneSettings.TranslationOffset.Z.Value));

                    var parentWorld = currentFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);
                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    animationToScale.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    animationToScale.DynamicFrames[frameIndex].Position[i] = bonePosition;

                    if (boneSettings.IsLocalOffset)
                    {
                        // Todo - Some inverse fuckery to children
                        var childBones = copyToSkeleton.GetDirectChildBones(i);
                    }
                }
            }
        }

        void FixAttachmentPoints(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToFix, AnimationClip animationToCopy)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    // Does this bone have a thing to fix?
                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;

                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (boneSettings.SelectedRelativeBone == null || mappedIndex == null)
                        continue;

                    var targetBoneIndex = mappedIndex.Value;

                    var currentCopyToFrame = AnimationSampler.Sample(frameIndex, 0, copyToSkeleton, animationToFix);
                    var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);


                    // self attach - The attachment point to move | copyToSkeleton -> boneIndex i
                    // target attach - the attachment point to move to |  copyFromSkeleton -> boneIndex targetBoneIndex
                    // self hand - Reference point | copyToSkeleton -> boneIndex SelectedRelativeBone.index
                    // target hand-  reference point | copyFromSkeleton -> boneIndex self hand mapping index

                    var boneIndexAttachmentPointSelf = i;
                    var boneIndexHandSelf = boneSettings.SelectedRelativeBone.BoneIndex;


                    var boneIndexAttachmentPointSource = mappedIndex.Value;
                    var mappedIndexRef = BoneHelper_new.GetMappedIndex(_bones, boneIndexHandSelf);
                    var boneIndexHandSource = mappedIndexRef;


                    var self = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, boneIndexAttachmentPointSource);
                    var hand = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, boneIndexHandSource.Value);

                    self.Decompose(out var _, out var _, out var bone0);
                    hand.Decompose(out var _, out var _, out var bone1);

                    var diff = bone0 - bone1;

                    var desiredBonePosWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, boneIndexHandSelf);

                    desiredBonePosWorld = /*MathUtil.CreateRotation(new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value)) **/
                      desiredBonePosWorld *
                       Matrix.CreateTranslation(diff);

                    // Reapply offsets
                    desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value)) *
                        desiredBonePosWorld *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.TranslationOffset.X.Value, (float)boneSettings.TranslationOffset.Y.Value, (float)boneSettings.TranslationOffset.Z.Value));

                    //   desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, targetBoneIndex) * Matrix.CreateScale(1);


                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);

                    var parentWorld = currentCopyToFrame.GetSkeletonAnimatedWorld(copyToSkeleton, fromParentBoneIndex);

                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    //animationToFix.DynamicFrames[frameIndex].Rotation[i] = boneRotation;
                    animationToFix.DynamicFrames[frameIndex].Position[i] = bonePosition;
                }
            }
        }

        void ApplyAnimationScale(AnimationClip animation, GameSkeleton copyToSkeleton)
        {
            var frameCount = animation.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                animation.DynamicFrames[frameIndex].Scale[0] = new Vector3((float)_settings.SkeletonScale);
            }


        }

        void ApplyBoneLengthMult(AnimationClip animation, GameSkeleton copyToSkeleton)
        {
            var frameCount = animation.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var boneIndex = 0; boneIndex < copyToSkeleton.BoneCount; boneIndex++)
                {
                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, boneIndex);
                    if (boneSettings == null)
                        continue;

                    animation.DynamicFrames[frameIndex].Position[boneIndex] = animation.DynamicFrames[frameIndex].Position[boneIndex] * (float)boneSettings.BoneLengthMult;
                }
            }
        }

        void FreezeBones(GameSkeleton copyToSkeleton, AnimationClip animation)
        {
            var frameCount = animation.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (mappedIndex != null)
                    {
                        var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                        if (boneSettings.FreezeTranslation)
                            animation.DynamicFrames[frameIndex].Position[i] = Vector3.Zero;

                        if (boneSettings.FreezeRotation)
                            animation.DynamicFrames[frameIndex].Rotation[i] = Quaternion.Identity;
                        if (boneSettings.FreezeRotationZ)
                            animation.DynamicFrames[frameIndex].Rotation[i] = new Quaternion(0, 0, animation.DynamicFrames[0].Rotation[i].Z, animation.DynamicFrames[0].Rotation[i].W);
                    }
                    else
                    {
                        if (_settings.ZeroUnmappedBones)
                        {
                            animation.DynamicFrames[frameIndex].Rotation[i] = Quaternion.Identity;
                            animation.DynamicFrames[frameIndex].Position[i] = Vector3.Zero;
                        }
                    }


                }
            }
        }


        AnimationClip CreateNewAnimation(GameSkeleton skeleton, AnimationClip animationToCopy)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;

            var newAnimation = new AnimationClip();
            newAnimation.PlayTimeInSec = animationToCopy.PlayTimeInSec;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                newAnimation.DynamicFrames.Add(new AnimationClip.KeyFrame());
                for (var i = 0; i < skeleton.BoneCount; i++)
                {
                    newAnimation.DynamicFrames[frameIndex].Rotation.Add(skeleton.Rotation[i]);
                    newAnimation.DynamicFrames[frameIndex].Position.Add(skeleton.Translation[i]);
                    newAnimation.DynamicFrames[frameIndex].Scale.Add(Vector3.One);
                }
            }

            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                if (newAnimation.DynamicFrames.Count != 0)
                    newAnimation.DynamicFrames[0].Scale[0] = Vector3.One;
            }
            return newAnimation;
        }
    }

    public static class BoneHelper_new
    {
        public static SkeletonBoneNode_new? GetBoneFromId(IEnumerable<SkeletonBoneNode_new> root, int boneId)
        {
            foreach (SkeletonBoneNode_new item in root)
            {
                if (item.BoneIndex == boneId)
                    return item;

                var result = GetBoneFromId(item.Children, boneId);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static int? GetMappedIndex(IEnumerable<SkeletonBoneNode_new> bones, int boneId)
        {
            var bone = GetBoneFromId(bones, boneId);
            if (bone == null || bone.HasMapping == false)
                return null;

            return bone.MappedIndex;
        }
    }
}
