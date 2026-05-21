using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Services;

namespace Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    public class ConvertCampaignAnimationCommand(IStandardDialogs standardDialogs) : IAeCommand
    {
        public bool Execute(AnimationClip? sourceAnimation, SkeletonBoneNode? rootBone, out AnimationClip? convertedAnimation)
        {
            convertedAnimation = null;

            if (sourceAnimation == null)
            {
                standardDialogs.ShowDialogBox("Unable to convert animation - No animation selected");
                return false;
            }

            if (rootBone == null)
            {
                standardDialogs.ShowDialogBox("Unable to convert animation - No root bone selected");
                return false;
            }

            var animationCopy = sourceAnimation.Clone();
            if (animationCopy.DynamicFrames.Count == 0)
            {
                standardDialogs.ShowDialogBox("Unable to convert animation - Animation has no frames");
                return false;
            }

            for (var frameIndex = 0; frameIndex < animationCopy.DynamicFrames.Count; frameIndex++)
            {
                var frame = animationCopy.DynamicFrames[frameIndex];
                var boneCount = frame.GetBoneCountFromFrame();

                if (rootBone.BoneIndex < 0 || rootBone.BoneIndex >= boneCount)
                {
                    standardDialogs.ShowDialogBox($"Unable to convert animation - Bone index {rootBone.BoneIndex} is out of range for frame {frameIndex}");
                    return false;
                }

                frame.Position[rootBone.BoneIndex] = Vector3.Zero;
                frame.Rotation[rootBone.BoneIndex] = Quaternion.Identity;
            }

            convertedAnimation = animationCopy;
            return true;
        }
    }
}
