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
            // Resampling to the same frame count changes nothing but adds float drift
            AnimationClip resampledAnimationToCopy;
            if (newFrameCount == animationToCopy.DynamicFrames.Count)
            {
                resampledAnimationToCopy = animationToCopy.Clone();
                resampledAnimationToCopy.PlayTimeInSec = newPlayTime;
            }
            else
            {
                resampledAnimationToCopy = GameWorld.Core.Animation.AnimationEditor.ReSample(copyFromSkeleton, animationToCopy, newFrameCount, newPlayTime);
            }
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


        // Walks the target bones parent first, keeping each bone's animated world as it goes, so a child is always
        // placed against its parent's new pose (and the frame is not resampled once per bone)
        void TransferAnimationWorld(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToCopy, AnimationClip newAnimation)
        {
            var processingOrder = GetParentFirstBoneOrder(copyToSkeleton);
            for (var frameIndex = 0; frameIndex < animationToCopy.DynamicFrames.Count; frameIndex++)
            {
                var sourcePose = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);
                var targetFrame = newAnimation.DynamicFrames[frameIndex];
                var targetWorlds = new Matrix[copyToSkeleton.BoneCount];
                foreach (var index in processingOrder)
                {
                    var parent = copyToSkeleton.GetParentBoneIndex(index);
                    var parentWorld = parent < 0 ? Matrix.Identity : targetWorlds[parent];
                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, index);
                    if (mappedIndex != null && mappedIndex.Value >= 0 && mappedIndex.Value < copyFromSkeleton.BoneCount)
                    {
                        var localTransform = sourcePose.GetSkeletonAnimatedWorld(copyFromSkeleton, mappedIndex.Value);
                        if (parent >= 0)
                            localTransform *= Matrix.Invert(parentWorld);
                        localTransform.Decompose(out _, out var rotation, out var position);

                        var settings = BoneHelper_new.GetBoneFromId(_bones, index);
                        if (settings?.ApplyRotation == true)
                            targetFrame.Rotation[index] = rotation;
                        if (settings?.ApplyTranslation == true)
                            targetFrame.Position[index] = position;
                    }
                    targetWorlds[index] = GetLocalTransform(targetFrame, index) * parentWorld;
                }
            }
        }

        static List<int> GetParentFirstBoneOrder(GameSkeleton skeleton)
        {
            var remaining = Enumerable.Range(0, skeleton.BoneCount).ToHashSet();
            var result = new List<int>(skeleton.BoneCount);
            while (remaining.Count > 0)
            {
                var ready = remaining
                    .Where(index => skeleton.GetParentBoneIndex(index) is var parent && (parent < 0 || !remaining.Contains(parent)))
                    .OrderBy(index => index)
                    .ToArray();
                if (ready.Length == 0)
                {
                    result.AddRange(remaining.OrderBy(index => index));   // a parent loop; shouldn't happen
                    break;
                }

                foreach (var index in ready)
                {
                    result.Add(index);
                    remaining.Remove(index);
                }
            }
            return result;
        }

        // Scales the translation of bones whose translation is transferred, by target bone length over source bone length
        void ApplyRelativeScale(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale)
        {
            for (var index = 0; index < copyToSkeleton.BoneCount; index++)
            {
                var settings = BoneHelper_new.GetBoneFromId(_bones, index);
                var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, index);
                if (mappedIndex == null || settings?.ApplyTranslation != true)
                    continue;
                var sourceParent = copyFromSkeleton.GetParentBoneIndex(mappedIndex.Value);
                var targetParent = copyToSkeleton.GetParentBoneIndex(index);
                if (sourceParent < 0 || targetParent < 0)
                    continue;

                var sourceLength = Vector3.Distance(copyFromSkeleton.GetWorldTransform(mappedIndex.Value).Translation, copyFromSkeleton.GetWorldTransform(sourceParent).Translation);
                var targetLength = Vector3.Distance(copyToSkeleton.GetWorldTransform(index).Translation, copyToSkeleton.GetWorldTransform(targetParent).Translation);
                if (sourceLength == 0 || targetLength == 0)
                    continue;

                var ratio = targetLength / sourceLength;
                foreach (var frame in animationToScale.DynamicFrames)
                    frame.Position[index] *= ratio;
            }
        }

        // The animated world of a bone, from the frame's local values
        static Matrix GetAnimatedWorldTransform(GameSkeleton skeleton, AnimationClip.KeyFrame frame, int boneIndex)
        {
            var worldTransform = GetLocalTransform(frame, boneIndex);
            var parentBoneIndex = skeleton.GetParentBoneIndex(boneIndex);
            while (parentBoneIndex != -1)
            {
                worldTransform *= GetLocalTransform(frame, parentBoneIndex);
                parentBoneIndex = skeleton.GetParentBoneIndex(parentBoneIndex);
            }
            return worldTransform;
        }

        static Matrix GetLocalTransform(AnimationClip.KeyFrame frame, int boneIndex) =>
            Matrix.CreateScale(frame.Scale[boneIndex]) *
            Matrix.CreateFromQuaternion(Quaternion.Normalize(frame.Rotation[boneIndex])) *
            Matrix.CreateTranslation(frame.Position[boneIndex]);

        void SnapBonesToWorld(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToScale, AnimationClip animationToCopy)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);
                var targetFrame = animationToScale.DynamicFrames[frameIndex];

                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;
                    if (boneSettings.ForceSnapToWorld == false)
                        continue;

                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (mappedIndex == null || mappedIndex.Value < 0 || mappedIndex.Value >= copyFromSkeleton.BoneCount)
                        continue;

                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var desiredBonePosWorld = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, mappedIndex.Value);
                    var parentWorld = GetAnimatedWorldTransform(copyToSkeleton, targetFrame, fromParentBoneIndex);

                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    // Apply the values to the animation
                    targetFrame.Rotation[i] = boneRotation;
                    targetFrame.Position[i] = bonePosition;
                }
            }
        }

        void ApplyOffsets(GameSkeleton copyToSkeleton, AnimationClip animationToScale)
        {
            var frameCount = animationToScale.DynamicFrames.Count;
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var targetFrame = animationToScale.DynamicFrames[frameIndex];
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    if (fromParentBoneIndex == -1)
                        continue;

                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;

                    var rotationOffset = new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value);
                    var translationOffset = new Vector3((float)boneSettings.TranslationOffset.X.Value, (float)boneSettings.TranslationOffset.Y.Value, (float)boneSettings.TranslationOffset.Z.Value);
                    if (rotationOffset == Vector3.Zero && translationOffset == Vector3.Zero)
                        continue;

                    var desiredBonePosWorld = MathUtil.CreateRotation(rotationOffset) *
                        GetAnimatedWorldTransform(copyToSkeleton, targetFrame, i) *
                        Matrix.CreateTranslation(translationOffset);

                    var parentWorld = GetAnimatedWorldTransform(copyToSkeleton, targetFrame, fromParentBoneIndex);
                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var boneRotation, out var bonePosition);

                    targetFrame.Rotation[i] = boneRotation;
                    targetFrame.Position[i] = bonePosition;

                    if (boneSettings.IsLocalOffset)
                    {
                        // Todo - Some inverse fuckery to children
                        var childBones = copyToSkeleton.GetDirectChildBones(i);
                    }
                }
            }
        }

        // A bone with a relative bone (a weapon slot relative to its hand) keeps the source's offset from that bone.
        // The offset is taken in the hand's own space, so it turns with the target's hand rather than staying fixed in world.
        void FixAttachmentPoints(GameSkeleton copyFromSkeleton, GameSkeleton copyToSkeleton, AnimationClip animationToFix, AnimationClip animationToCopy)
        {
            var frameCount = animationToCopy.DynamicFrames.Count;

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var copyFromFrame = AnimationSampler.Sample(frameIndex, 0, copyFromSkeleton, animationToCopy);
                var targetFrame = animationToFix.DynamicFrames[frameIndex];
                for (var i = 0; i < copyToSkeleton.BoneCount; i++)
                {
                    // Does this bone have a thing to fix?
                    var boneSettings = BoneHelper_new.GetBoneFromId(_bones, i);
                    if (boneSettings == null)
                        continue;

                    var mappedIndex = BoneHelper_new.GetMappedIndex(_bones, i);
                    if (boneSettings.SelectedRelativeBone == null || mappedIndex == null)
                        continue;

                    // self attach - The attachment point to move | copyToSkeleton -> boneIndex i
                    // target attach - the attachment point to move to |  copyFromSkeleton -> boneIndex mappedIndex
                    // self hand - Reference point | copyToSkeleton -> boneIndex SelectedRelativeBone.index
                    // target hand-  reference point | copyFromSkeleton -> boneIndex self hand mapping index
                    var fromParentBoneIndex = copyToSkeleton.GetParentBoneIndex(i);
                    var boneIndexHandSelf = boneSettings.SelectedRelativeBone.BoneIndex;
                    if (fromParentBoneIndex == -1 || boneIndexHandSelf == i || boneIndexHandSelf < 0 || boneIndexHandSelf >= copyToSkeleton.BoneCount ||
                        mappedIndex.Value < 0 || mappedIndex.Value >= copyFromSkeleton.BoneCount)
                        continue;

                    var boneIndexHandSource = BoneHelper_new.GetMappedIndex(_bones, boneIndexHandSelf);
                    if (boneIndexHandSource == null || boneIndexHandSource.Value < 0 || boneIndexHandSource.Value >= copyFromSkeleton.BoneCount)
                        continue;

                    var self = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, mappedIndex.Value);
                    var hand = copyFromFrame.GetSkeletonAnimatedWorld(copyFromSkeleton, boneIndexHandSource.Value);
                    (self * Matrix.Invert(hand)).Decompose(out var _, out var _, out var offsetInHandSpace);

                    var desiredBonePosWorld = Matrix.CreateTranslation(offsetInHandSpace) * GetAnimatedWorldTransform(copyToSkeleton, targetFrame, boneIndexHandSelf);

                    // Reapply offsets
                    desiredBonePosWorld = MathUtil.CreateRotation(new Vector3((float)boneSettings.RotationOffset.X.Value, (float)boneSettings.RotationOffset.Y.Value, (float)boneSettings.RotationOffset.Z.Value)) *
                        desiredBonePosWorld *
                        Matrix.CreateTranslation(new Vector3((float)boneSettings.TranslationOffset.X.Value, (float)boneSettings.TranslationOffset.Y.Value, (float)boneSettings.TranslationOffset.Z.Value));

                    var parentWorld = GetAnimatedWorldTransform(copyToSkeleton, targetFrame, fromParentBoneIndex);
                    var bonePositionLocalSpace = desiredBonePosWorld * Matrix.Invert(parentWorld);
                    bonePositionLocalSpace.Decompose(out var _, out var _, out var bonePosition);

                    targetFrame.Position[i] = bonePosition;
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
                        if (boneSettings == null)
                            continue;

                        if (boneSettings.FreezeTranslation)
                            animation.DynamicFrames[frameIndex].Position[i] = Vector3.Zero;

                        if (boneSettings.FreezeRotation)
                            animation.DynamicFrames[frameIndex].Rotation[i] = Quaternion.Identity;
                        if (boneSettings.FreezeRotationZ)
                        {
                            // Keep the swing, hold the twist around Z at the first frame's
                            var currentRotation = Quaternion.Normalize(animation.DynamicFrames[frameIndex].Rotation[i]);
                            var currentSwing = currentRotation * Quaternion.Inverse(ExtractTwistAroundZ(currentRotation));
                            var firstFrameTwist = ExtractTwistAroundZ(animation.DynamicFrames[0].Rotation[i]);
                            animation.DynamicFrames[frameIndex].Rotation[i] = Quaternion.Normalize(currentSwing * firstFrameTwist);
                        }
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

        // The twist part of a swing/twist decomposition around Z
        static Quaternion ExtractTwistAroundZ(Quaternion rotation)
        {
            var lengthSquared = rotation.Z * rotation.Z + rotation.W * rotation.W;
            if (lengthSquared <= float.Epsilon)
                return Quaternion.Identity;

            var inverseLength = 1.0f / MathF.Sqrt(lengthSquared);
            return new Quaternion(0, 0, rotation.Z * inverseLength, rotation.W * inverseLength);
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
