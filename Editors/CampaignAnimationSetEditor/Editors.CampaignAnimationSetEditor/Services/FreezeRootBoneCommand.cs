using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Services;

namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>Clones a clip and zeroes one bone's local position/rotation on every frame, turning
    /// a battle movement animation into an in-place loop. The campaign map drives movement
    /// externally rather than through the animation's root bone. A fixed axis swap held by a
    /// parentless root (pass the skeleton to detect it) is kept rather than zeroed.</summary>
    public class FreezeRootBoneCommand(IStandardDialogs standardDialogs)
    {
        public bool Execute(AnimationClip? sourceAnimation, int rootBoneIndex, string rootBoneName, out AnimationClip? convertedAnimation, GameSkeleton? skeleton = null)
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
                var boneCount = animationCopy.DynamicFrames[frameIndex].GetBoneCountFromFrame();
                if (rootBoneIndex < 0 || rootBoneIndex >= boneCount)
                {
                    standardDialogs.ShowDialogBox($"Unable to convert animation - Bone index {rootBoneIndex} is out of range for frame {frameIndex}");
                    return false;
                }
            }

            // Some rigs hold a fixed axis swap on the root for the whole clip: vanilla war machines (carriage01,
            // corpsecart01, the campaign doomwheel/war altar/blood cauldron...) and rigs exported from Blender.
            // Zeroing it would tip the model over by 90 degrees. CA's own campaign animations for those rigs keep
            // that rotation on animroot (e.g. cam_wm_bloodcauldron01_walk_01), so it is kept; only movement and a
            // changing heading are removed.
            var keptRotation = (skeleton != null ? GetFixedAxisSwap(animationCopy, skeleton, rootBoneIndex) : null) ?? Quaternion.Identity;

            foreach (var frame in animationCopy.DynamicFrames)
            {
                frame.Position[rootBoneIndex] = Vector3.Zero;
                frame.Rotation[rootBoneIndex] = keptRotation;
            }

            convertedAnimation = animationCopy;
            return true;
        }

        /// <summary>The root's rotation, if it is the same on every frame, swaps world axes and moves the
        /// up axis, and the root has no parent. Null otherwise.</summary>
        static Quaternion? GetFixedAxisSwap(AnimationClip animation, GameSkeleton skeleton, int rootBoneIndex)
        {
            if (rootBoneIndex >= skeleton.BoneCount || skeleton.GetParentBoneIndex(rootBoneIndex) != -1)
                return null;
            if (animation.DynamicFrames.Any(frame => frame.Rotation.Count != skeleton.BoneCount || frame.Position.Count != skeleton.BoneCount))
                return null;

            var rotation = animation.DynamicFrames[0].Rotation[rootBoneIndex];
            if (!float.IsFinite(rotation.LengthSquared()) || rotation.LengthSquared() <= float.Epsilon)
                return null;
            rotation.Normalize();

            var basis = Matrix.CreateFromQuaternion(rotation);
            if (basis.Up.Y >= 1 - 0.00001f || !IsAxisAligned(basis.Right) || !IsAxisAligned(basis.Up) || !IsAxisAligned(basis.Backward))
                return null;

            foreach (var frame in animation.DynamicFrames)
            {
                var frameRotation = frame.Rotation[rootBoneIndex];
                if (!float.IsFinite(frameRotation.LengthSquared()) || frameRotation.LengthSquared() <= float.Epsilon)
                    return null;
                frameRotation.Normalize();

                // q and -q are the same rotation; allow for the file's quantisation
                if (MathF.Min((frameRotation - rotation).LengthSquared(), (frameRotation + rotation).LengthSquared()) > 0.00000001f)
                    return null;
            }

            return rotation;
        }

        static bool IsAxisAligned(Vector3 axis)
        {
            const float tolerance = 0.00001f;
            var x = MathF.Abs(axis.X);
            var y = MathF.Abs(axis.Y);
            var z = MathF.Abs(axis.Z);
            return (MathF.Abs(x - 1) < tolerance && y < tolerance && z < tolerance)
                || (MathF.Abs(y - 1) < tolerance && x < tolerance && z < tolerance)
                || (MathF.Abs(z - 1) < tolerance && x < tolerance && y < tolerance);
        }
    }
}
