using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Services;

namespace Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    public class ConvertCampaignAnimationCommand(IStandardDialogs StandardDialogs) : IUiCommand
    {
        public bool Execute(AnimationClip? sourceAnimation, SkeletonBoneNode? rootBone, out AnimationClip? convertedAnimation, out string? errorText)
        {
            convertedAnimation = null;
            errorText = null;

            // if (_selectedUnit == null)
            // {
            //     MessageBox.Show("No model loaded");
            //     return;
            // }
            //
            // if (_selectedAnimationClip == null)
            // {
            //     MessageBox.Show("No animation selected");
            //     return;
            // }
            //
            // if (ModelBoneList.SelectedItem == null)
            // {
            //     MessageBox.Show("No root bone selected");
            //     return;
            // }

            //   MessageBox.Show(errorText ?? "Unable to convert animation");

            if (sourceAnimation == null)
            {
                errorText = "No animation selected";
                return false;
            }

            if (rootBone == null)
            {
                errorText = "No root bone selected";
                return false;
            }

            var animationCopy = sourceAnimation.Clone();
            if (animationCopy.DynamicFrames.Count == 0)
            {
                errorText = "Animation has no frames";
                return false;
            }

            for (var frameIndex = 0; frameIndex < animationCopy.DynamicFrames.Count; frameIndex++)
            {
                var frame = animationCopy.DynamicFrames[frameIndex];
                var boneCount = frame.GetBoneCountFromFrame();

                if (rootBone.BoneIndex < 0 || rootBone.BoneIndex >= boneCount)
                {
                    errorText = $"Bone index {rootBone.BoneIndex} is out of range for frame {frameIndex}";
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
