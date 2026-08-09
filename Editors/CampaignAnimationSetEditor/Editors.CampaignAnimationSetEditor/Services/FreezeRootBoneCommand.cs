using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Services;

namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>Clones a clip and zeroes one bone's local position/rotation on every frame, turning
    /// a battle movement animation into an in-place loop. The campaign map drives movement
    /// externally rather than through the animation's root bone.</summary>
    public class FreezeRootBoneCommand(IStandardDialogs standardDialogs)
    {
        public bool Execute(AnimationClip? sourceAnimation, int rootBoneIndex, string rootBoneName, out AnimationClip? convertedAnimation)
        {
            convertedAnimation = null;

            if (sourceAnimation == null)
            {
                standardDialogs.ShowDialogBox("Unable to convert animation - No animation selected");
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

                if (rootBoneIndex < 0 || rootBoneIndex >= boneCount)
                {
                    standardDialogs.ShowDialogBox($"Unable to convert animation - Bone index {rootBoneIndex} is out of range for frame {frameIndex}");
                    return false;
                }

                frame.Position[rootBoneIndex] = Vector3.Zero;
                frame.Rotation[rootBoneIndex] = Quaternion.Identity;
            }

            convertedAnimation = animationCopy;
            return true;
        }
    }
}
