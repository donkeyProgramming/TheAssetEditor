using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Filetypes.RigidModel.AnimationFile;

namespace View3D.Animation
{
    public class AnimationSampler
    {


        public static AnimationFrame Sample(int frameIndex, float frameIterpolation, GameSkeleton skeleton, List<AnimationClip> animationClips, bool applyStaticFrame = true, bool applyDynamicFrames = true)
        {
            try
            {
                if (skeleton == null)
                    return null;

                var currentFrame = new AnimationFrame();
                for (int i = 0; i < skeleton.BoneCount; i++)
                {
                    currentFrame.BoneTransforms.Add(new AnimationFrame.BoneKeyFrame()
                    {
                        Translation = skeleton.Translation[i],
                        Rotation = skeleton.Rotation[i],
                        BoneIndex = i,
                        ParentBoneIndex = skeleton.GetParentBone(i),
                    });
                }

                if (animationClips != null)
                {
                    if (applyStaticFrame)
                    {
                        foreach (var animation in animationClips)
                        {
                            if (animation.UseStaticFrame)
                                ApplyAnimation(animation.StaticFrame, null, 0, currentFrame, animation.RotationMappings, animation.TranslationMappings, AnimationBoneMappingType.Static);
                        }
                    }

                    if (applyDynamicFrames)
                    {
                        if (animationClips.Any() && animationClips[0].UseDynamicFames)
                        {
                            if (animationClips[0].DynamicFrames.Count > frameIndex)
                            {
                                var currentFrameKeys = GetKeyFrameFromIndex(animationClips[0].DynamicFrames, frameIndex);
                                var nextFrameKeys = GetKeyFrameFromIndex(animationClips[0].DynamicFrames, frameIndex + 1);
                                ApplyAnimation(currentFrameKeys, nextFrameKeys, (float)frameIterpolation, currentFrame, animationClips[0].RotationMappings, animationClips[0].TranslationMappings, AnimationBoneMappingType.Dynamic);
                            }
                        }
                    }
                }

                for (int i = 0; i < currentFrame.BoneTransforms.Count(); i++)
                {
                    Quaternion rotation = currentFrame.BoneTransforms[i].Rotation;
                    Vector3 translation = currentFrame.BoneTransforms[i].Translation;
                    currentFrame.BoneTransforms[i].WorldTransform = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);

                    var parentindex = currentFrame.BoneTransforms[i].ParentBoneIndex;
                    if (parentindex == -1)
                    {
                        var scale = Matrix.CreateScale(-1, 1, 1);
                        currentFrame.BoneTransforms[i].WorldTransform = (scale * currentFrame.BoneTransforms[i].WorldTransform);
                        continue;
                    }

                    currentFrame.BoneTransforms[i].WorldTransform = currentFrame.BoneTransforms[i].WorldTransform * currentFrame.BoneTransforms[parentindex].WorldTransform;
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

        public static AnimationFrame Sample(float t, GameSkeleton skeleton, List<AnimationClip> animationClips, bool applyStaticFrame, bool applyDynamicFrames)
        {
            try
            {
                if (skeleton == null)
                    return null;

                // Make sure it its in the 0-1 range
                t = EnsureRange(t, 0, 1);

                int frameIndex = 0;
                float frameIterpolation = 0;
                if (animationClips != null)
                {
                    if (applyDynamicFrames)
                    {
                        int maxFrames = animationClips[0].DynamicFrames.Count() - 1;
                        float frame = maxFrames * t;

                        frameIndex = (int)(frame);
                        frameIterpolation = frame - frameIndex;
                    }
                }

                return Sample(frameIndex, frameIterpolation, skeleton, animationClips, applyStaticFrame, applyDynamicFrames);
            }
            catch (Exception e)
            {
                ILogger logger = Logging.Create<AnimationSampler>();
                logger.Error(e.Message);
                throw;
            }
        }

        public static AnimationFrame Sample(int frameIndex, float frameIterpolation, GameSkeleton skeleton, AnimationClip animationClip, bool applyStaticFrame = true, bool applyDynamicFrames = true)
        {
            return Sample(frameIndex, frameIterpolation, skeleton, new List<AnimationClip>() { animationClip }, applyStaticFrame, applyDynamicFrames);
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
