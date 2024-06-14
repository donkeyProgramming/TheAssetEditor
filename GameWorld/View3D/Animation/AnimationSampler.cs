using GameWorld.Core.Animation.AnimationChange;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Animation
{
    public class AnimationSampler
    {
        public static AnimationFrame Sample(int frameIndex, float frameIterpolation, GameSkeleton skeleton, AnimationClip animationClip, List<IAnimationChangeRule> animationChangeRules = null, bool freezeFrame = false)
        {
            try
            {
                if (skeleton == null)
                    return null;

                var currentFrame = skeleton.ConvertToAnimationFrame();

                // Apply the animation to the skeleton frame
                if (animationClip != null)
                {
                    if (animationClip.DynamicFrames.Count > frameIndex)
                    {
                        var currentFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndex);
                        var nextFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndex + 1);
                        InterpolateFrame(currentFrameKeys, nextFrameKeys, frameIterpolation, currentFrame, freezeFrame);
                    }
                }

                // Compute the worldspace values
                for (var boneIndex = 0; boneIndex < currentFrame.BoneTransforms.Count(); boneIndex++)
                {
                    currentFrame.BoneTransforms[boneIndex].ComputeWorldMatrixFromComponents();
                    if (animationChangeRules != null)
                    {
                        foreach (var rule in animationChangeRules.OfType<ILocalSpaceAnimationRule>())
                            rule.TransformFrameLocalSpace(currentFrame, boneIndex, animationClip.PlayTimeInSec);
                    }

                    var parentindex = currentFrame.BoneTransforms[boneIndex].ParentBoneIndex;
                    if (parentindex == -1)
                        continue;

                    currentFrame.BoneTransforms[boneIndex].WorldTransform = currentFrame.BoneTransforms[boneIndex].WorldTransform * currentFrame.BoneTransforms[parentindex].WorldTransform;
                }

                // Apply animation rules
                if (animationChangeRules != null)
                {
                    foreach (var rule in animationChangeRules.OfType<IWorldSpaceAnimationRule>())
                        rule.TransformFrameWorldSpace(currentFrame, animationClip.PlayTimeInSec);
                }

                // Remove the skeleten info from the world transform.
                // This is applied again in the animation shader.
                for (var boneIndex = 0; boneIndex < skeleton.BoneCount; boneIndex++)
                {
                    var inv = Matrix.Invert(skeleton.GetWorldTransform(boneIndex));
                    currentFrame.BoneTransforms[boneIndex].WorldTransform = Matrix.Multiply(inv, currentFrame.BoneTransforms[boneIndex].WorldTransform);
                }

                return currentFrame;
            }
            catch (Exception e)
            {
                var logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }

        public static AnimationFrame SampleBetween2Frames(int frameIndexA, int frameIndexB, float frameIterpolation, GameSkeleton skeleton, AnimationClip animationClip, List<IAnimationChangeRule> animationChangeRules = null)
        {
            try
            {
                frameIterpolation = MathUtil.EnsureRange(frameIterpolation, -1, 1);
                if (skeleton == null)
                    return null;

                var currentFrame = skeleton.ConvertToAnimationFrame();

                // Apply the animation to the skeleton frame
                if (animationClip != null)
                {
                    if (animationClip.DynamicFrames.Count > frameIndexA && animationClip.DynamicFrames.Count > frameIndexB)
                    {
                        var currentFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndexA);
                        var nextFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndexB);
                        InterpolateFrame(currentFrameKeys, nextFrameKeys, frameIterpolation, currentFrame, false);
                    }
                }

                // Compute the worldspace values
                for (var boneIndex = 0; boneIndex < currentFrame.BoneTransforms.Count(); boneIndex++)
                {
                    currentFrame.BoneTransforms[boneIndex].ComputeWorldMatrixFromComponents();
                    if (animationChangeRules != null)
                    {
                        foreach (var rule in animationChangeRules.OfType<ILocalSpaceAnimationRule>())
                            rule.TransformFrameLocalSpace(currentFrame, boneIndex, animationClip.PlayTimeInSec);
                    }

                    var parentindex = currentFrame.BoneTransforms[boneIndex].ParentBoneIndex;
                    if (parentindex == -1)
                        continue;

                    currentFrame.BoneTransforms[boneIndex].WorldTransform = currentFrame.BoneTransforms[boneIndex].WorldTransform * currentFrame.BoneTransforms[parentindex].WorldTransform;
                }

                // Apply animation rules
                if (animationChangeRules != null)
                {
                    foreach (var rule in animationChangeRules.OfType<IWorldSpaceAnimationRule>())
                        rule.TransformFrameWorldSpace(currentFrame, animationClip.PlayTimeInSec);
                }

                // Remove the skeleten info from the world transform.
                // This is applied again in the animation shader.
                for (var boneIndex = 0; boneIndex < skeleton.BoneCount; boneIndex++)
                {
                    var inv = Matrix.Invert(skeleton.GetWorldTransform(boneIndex));
                    currentFrame.BoneTransforms[boneIndex].WorldTransform = Matrix.Multiply(inv, currentFrame.BoneTransforms[boneIndex].WorldTransform);
                }

                return currentFrame;
            }
            catch (Exception e)
            {
                var logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }


        public static AnimationFrame Sample(float t_between_0_and_1, GameSkeleton skeleton, AnimationClip animationClip, List<IAnimationChangeRule> animationChangeRules = null, bool freezeFrame = false)
        {
            try
            {
                var clampedT = MathUtil.EnsureRange(t_between_0_and_1, 0, 1);
                var frameIndex = 0;
                float frameIterpolation = 0;

                if (animationClip != null)
                {
                    var maxFrames = animationClip.DynamicFrames.Count() - 1;
                    if (maxFrames < 0)
                        maxFrames = 0;
                    var frameWithLeftover = maxFrames * clampedT;
                    var clampedFrame = (float)Math.Round(frameWithLeftover);

                    frameIndex = (int)clampedFrame;
                    frameIterpolation = frameWithLeftover - clampedFrame;
                }
                return Sample(frameIndex, frameIterpolation, skeleton, animationClip, animationChangeRules, freezeFrame);
            }
            catch (Exception e)
            {
                var logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }

        static AnimationClip.KeyFrame GetKeyFrameFromIndex(List<AnimationClip.KeyFrame> keyframes, int frameIndex)
        {
            var count = keyframes.Count();
            if (frameIndex >= count)
                return null;

            return keyframes[frameIndex];
        }

        static Quaternion ComputeRotationCurrentFrame(int boneIndex, AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation)
        {
            var animationValueCurrentFrame = currentFrame.Rotation[boneIndex];
            if (nextFrame != null)
                animationValueCurrentFrame = Quaternion.Slerp(animationValueCurrentFrame, nextFrame.Rotation[boneIndex], animationInterpolation);
            animationValueCurrentFrame.Normalize();
            return animationValueCurrentFrame;
        }

        static Vector3 ComputeTranslationCurrentFrame(int boneIndex, AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation)
        {
            var animationValueCurrentFrame = currentFrame.Position[boneIndex];
            if (nextFrame != null)
                animationValueCurrentFrame = Vector3.Lerp(animationValueCurrentFrame, nextFrame.Position[boneIndex], animationInterpolation);

            return animationValueCurrentFrame;
        }

        static Vector3 ComputeScaleCurrentFrame(int boneIndex, AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation)
        {
            var animationValueCurrentFrame = currentFrame.Scale[boneIndex];
            if (nextFrame != null)
                animationValueCurrentFrame = Vector3.Lerp(animationValueCurrentFrame, nextFrame.Scale[boneIndex], animationInterpolation);

            return animationValueCurrentFrame;
        }

        static void InterpolateFrame(AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation, AnimationFrame finalAnimationFrame, bool freezeFrame = false)
        {
            if (currentFrame == null)
                return;

            var skeletonBoneCount = finalAnimationFrame.BoneTransforms.Count();
            var animBoneCount = currentFrame.GetBoneCountFromFrame();
            var boneCount = Math.Min(skeletonBoneCount, animBoneCount);
            if (freezeFrame)
            {
                if (animationInterpolation < 0) animationInterpolation = 0;
                if (animationInterpolation > 0) animationInterpolation = 1;
            }
            for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                finalAnimationFrame.BoneTransforms[boneIndex].Translation = ComputeTranslationCurrentFrame(boneIndex, currentFrame, nextFrame, animationInterpolation);
                finalAnimationFrame.BoneTransforms[boneIndex].Rotation = ComputeRotationCurrentFrame(boneIndex, currentFrame, nextFrame, animationInterpolation);
                finalAnimationFrame.BoneTransforms[boneIndex].Scale = ComputeScaleCurrentFrame(boneIndex, currentFrame, nextFrame, animationInterpolation);
            }
        }
    }
}
