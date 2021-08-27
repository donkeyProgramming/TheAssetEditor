using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation.AnimationChange;
using View3D.SceneNodes;
using static Filetypes.RigidModel.AnimationFile;

namespace View3D.Animation
{
    public class AnimationSampler
    {
        public static AnimationFrame Sample(int frameIndex, float frameIterpolation, GameSkeleton skeleton, AnimationClip animationClip, List<AnimationChangeRule> animationChangeRules = null, Nullable<float> time = null)
        {
            try
            {

                if (skeleton == null)
                    return null;

                var currentFrame = skeleton.CreateAnimationFrame();

                if (animationClip != null)
                {
                    ApplyAnimation(animationClip.StaticFrame, null, 0, currentFrame, animationClip.RotationMappings, animationClip.TranslationMappings, AnimationBoneMappingType.Static);
                    
                    if (animationClip.DynamicFrames.Count > frameIndex)
                    {
                        var currentFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndex);
                        var nextFrameKeys = GetKeyFrameFromIndex(animationClip.DynamicFrames, frameIndex + 1);
                        ApplyAnimation(currentFrameKeys, nextFrameKeys, frameIterpolation, currentFrame, animationClip.RotationMappings, animationClip.TranslationMappings, AnimationBoneMappingType.Dynamic);

                        // Apply skeleton scale
                        for (int i = 0; i < currentFrame.BoneTransforms.Count(); i++)
                            currentFrame.BoneTransforms[i].Scale = animationClip.DynamicFrames[0].Scale[i];
                    }
                }
          
                for (int i = 0; i < currentFrame.BoneTransforms.Count(); i++)
                {
                    Quaternion rotation = currentFrame.BoneTransforms[i].Rotation;
                    Vector3 translation = currentFrame.BoneTransforms[i].Translation;

                    currentFrame.BoneTransforms[i].WorldTransform =
                        Matrix.CreateScale(currentFrame.BoneTransforms[i].Scale) * 
                        Matrix.CreateFromQuaternion(rotation) *
                        Matrix.CreateTranslation(translation);
               
                    if (animationChangeRules != null)
                    {
                        foreach (var rule in animationChangeRules)
                            rule.ApplyRule(currentFrame, i, time.Value * animationClip.PlayTimeInSec);
                    }

                    var parentindex = currentFrame.BoneTransforms[i].ParentBoneIndex;
                    if (parentindex == -1)
                        continue;

                    currentFrame.BoneTransforms[i].WorldTransform = currentFrame.BoneTransforms[i].WorldTransform * currentFrame.BoneTransforms[parentindex].WorldTransform;
                }

                if (animationChangeRules != null)
                {
                    foreach (var rule in animationChangeRules)
                        rule.ApplyRuleAfter(currentFrame, time.Value * animationClip.PlayTimeInSec);

                    //foreach (var rule in animationChangeRules)
                    //    rule.ApplyRule(currentFrame, 0);
                }


                for (int i = 0; i < skeleton.BoneCount; i++)
                {
                    var inv = Matrix.Invert(skeleton.GetWorldTransform(i));
                    currentFrame.BoneTransforms[i].WorldTransform = Matrix.Multiply(inv, currentFrame.BoneTransforms[i].WorldTransform);
                }

                return currentFrame;
            }
            catch (Exception e)
            {
                ILogger logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }

        public static AnimationFrame Sample(float t, GameSkeleton skeleton, AnimationClip animationClip, List<AnimationChangeRule> animationChangeRules = null)
        {
            try
            {
                var clampedT = EnsureRange(t, 0, 1);
                int frameIndex = 0;
                float frameIterpolation = 0;

                if (animationClip != null)
                {
                    int maxFrames = animationClip.DynamicFrames.Count() - 1;
                    if (maxFrames < 0)
                        maxFrames = 0;
                    float frameWithLeftover = maxFrames * clampedT;
                    float clampedFrame = (float)Math.Floor(frameWithLeftover);

                    frameIndex = (int)(clampedFrame);
                    frameIterpolation = frameWithLeftover - clampedFrame; 
                }

                return Sample(frameIndex, frameIterpolation, skeleton, animationClip, animationChangeRules, t);
            }
            catch (Exception e)
            {
                ILogger logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }


        static float EnsureRange(float value, float min, float max)
        {
            if (value > max)
                return max;
            else if (value < min)
                return min;
            return value;
        }

        static AnimationClip.KeyFrame GetKeyFrameFromIndex(List<AnimationClip.KeyFrame> keyframes, int frameIndex)
        {
            int count = keyframes.Count();
            if (frameIndex >= count)
                return null;

            return keyframes[frameIndex];
        }

        static Quaternion ComputeRotationsCurrentFrame(int boneIndex, AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation)
        {
            var animationValueCurrentFrame = currentFrame.Rotation[boneIndex];
            if (nextFrame != null)
            {
                var animationValueNextFrame = nextFrame.Rotation[boneIndex];
                animationValueCurrentFrame = Quaternion.Slerp(animationValueCurrentFrame, animationValueNextFrame, animationInterpolation);
            }
            animationValueCurrentFrame.Normalize();
            return animationValueCurrentFrame;
        }

        static Vector3 ComputeTranslationCurrentFrame(int boneIndex, AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation)
        {
            var animationValueCurrentFrame = currentFrame.Position[boneIndex];
            if (nextFrame != null)
            {
                var animationValueNextFrame = nextFrame.Position[boneIndex];
                animationValueCurrentFrame = Vector3.Lerp(animationValueCurrentFrame, animationValueNextFrame, animationInterpolation);
            }

            return animationValueCurrentFrame;
        }

        static void ApplyAnimation(AnimationClip.KeyFrame currentFrame, AnimationClip.KeyFrame nextFrame, float animationInterpolation,
            AnimationFrame finalAnimationFrame, List<AnimationBoneMapping> rotMapping, List<AnimationBoneMapping> transMapping, AnimationBoneMappingType boneMappingMode)
        {
            if (currentFrame == null)
                return;

            for (int i = 0; i < finalAnimationFrame.BoneTransforms.Count(); i++)
            {
                if (transMapping[i].MappingType == boneMappingMode)
                    finalAnimationFrame.BoneTransforms[i].Translation = ComputeTranslationCurrentFrame(transMapping[i].Id, currentFrame, nextFrame, animationInterpolation);

                if (rotMapping[i].MappingType == boneMappingMode)
                    finalAnimationFrame.BoneTransforms[i].Rotation = ComputeRotationsCurrentFrame(rotMapping[i].Id, currentFrame, nextFrame, animationInterpolation);
            }
        }
    }
}
