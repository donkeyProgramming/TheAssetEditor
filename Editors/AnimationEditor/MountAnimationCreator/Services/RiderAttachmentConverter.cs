using System;
using System.Linq;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;

namespace AnimationEditor.MountAnimationCreator.Services
{
    /// <summary>
    /// Converts a rider animation between WH3's two ways of riding. Synchronized: the rider's animroot moves with
    /// the mount's. Attached: the game glues the rider's animroot to an ap_ bone of the mount's tech skeleton, so
    /// it stays at the origin. Both need the mount animation, which moves the attach bone.
    /// </summary>
    public class RiderAttachmentConverter
    {
        readonly GameSkeleton _riderSkeleton;
        readonly GameSkeleton _mountSkeleton;
        readonly AnimationClip _mountClip;
        readonly int _attachParentBone;
        readonly Matrix _attachLocal;

        /// <param name="attachSkeleton">The mount's tech skeleton: the mount skeleton plus the attach bone.</param>
        public RiderAttachmentConverter(GameSkeleton riderSkeleton, GameSkeleton mountSkeleton, AnimationClip mountClip, GameSkeleton attachSkeleton, int attachBone)
        {
            _riderSkeleton = riderSkeleton;
            _mountSkeleton = mountSkeleton;
            _mountClip = mountClip;

            // The attach bone isn't animated: it rides at its rest transform on a parent the mount skeleton must share
            _attachParentBone = attachSkeleton.GetParentBoneIndex(attachBone);
            if (_attachParentBone < 0 || _attachParentBone >= mountSkeleton.BoneCount ||
                !string.Equals(attachSkeleton.BoneNames[_attachParentBone], mountSkeleton.BoneNames[_attachParentBone], StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The attach bone '{attachSkeleton.BoneNames[attachBone]}' hangs off a bone the mount skeleton '{mountSkeleton.SkeletonName}' doesn't have at the same index.");

            // Normalised: vanilla tech skeletons store it slightly off unit length
            _attachLocal = Matrix.CreateFromQuaternion(Quaternion.Normalize(attachSkeleton.Rotation[attachBone])) * Matrix.CreateTranslation(attachSkeleton.Translation[attachBone]);
        }

        /// <summary>The attach bone's world transform at a rider frame. The mount is sampled at the same normalised
        /// time, so clips of different lengths line up start to end.</summary>
        public Matrix AttachPointWorld(int riderFrame, int riderFrameCount)
        {
            var mountFrame = AnimationSampler.Sample(NormalisedTime(riderFrame, riderFrameCount), _mountSkeleton, _mountClip);
            return _attachLocal * mountFrame.GetSkeletonAnimatedWorld(_mountSkeleton, _attachParentBone);
        }

        Matrix MountRootWorld(int riderFrame, int riderFrameCount)
        {
            var mountFrame = AnimationSampler.Sample(NormalisedTime(riderFrame, riderFrameCount), _mountSkeleton, _mountClip);
            return mountFrame.GetSkeletonAnimatedWorld(_mountSkeleton, 0);
        }

        static float NormalisedTime(int frame, int frameCount) => frameCount <= 1 ? 0 : frame / (float)(frameCount - 1);

        /// <summary>Keeps the rider's pose relative to the attach bone and zeroes its animroot.</summary>
        public AnimationClip ToAttached(AnimationClip synchronized)
        {
            var output = synchronized.Clone();
            var frameCount = output.DynamicFrames.Count;
            for (var f = 0; f < frameCount; f++)
            {
                var frame = output.DynamicFrames[f];
                var animRoot = LocalMatrix(frame, 0);
                Reparent(frame, animRoot * Matrix.Invert(AttachPointWorld(f, frameCount)));

                frame.Position[0] = Vector3.Zero;
                frame.Rotation[0] = Quaternion.Identity;
            }
            return output;
        }

        /// <summary>Puts the rider where the attach bone holds it, under a new animroot that follows the mount's,
        /// offset to sit under the attach point.</summary>
        public AnimationClip ToSynchronized(AnimationClip attached)
        {
            var output = attached.Clone();
            var frameCount = output.DynamicFrames.Count;

            // The attach point's ground position relative to the mount's animroot, at the start
            var startOffset = (AttachPointWorld(0, frameCount) * Matrix.Invert(MountRootWorld(0, frameCount))).Translation;
            var groundOffset = Matrix.CreateTranslation(startOffset.X, 0, startOffset.Z);

            for (var f = 0; f < frameCount; f++)
            {
                var frame = output.DynamicFrames[f];
                var newAnimRoot = groundOffset * MountRootWorld(f, frameCount);
                var animRoot = LocalMatrix(frame, 0);
                Reparent(frame, animRoot * AttachPointWorld(f, frameCount) * Matrix.Invert(newAnimRoot));

                newAnimRoot.Decompose(out _, out var rotation, out var translation);
                frame.Position[0] = translation;
                frame.Rotation[0] = rotation;
            }
            return output;
        }

        /// <summary>Attached animations keep animroot at the origin on every frame; synchronized ones never do.</summary>
        public static bool IsAttached(AnimationClip clip) =>
            clip.DynamicFrames.All(f => f.Position[0].LengthSquared() < 1e-8f && Math.Abs(Math.Abs(Quaternion.Normalize(f.Rotation[0]).W) - 1) < 1e-5f);

        /// <summary>Slots vanilla attached sets keep synchronized: deaths and the RIDER_COMBAT_&lt;n&gt; kills.</summary>
        public static bool StaysSynchronized(string riderSlot) =>
            riderSlot.StartsWith("RIDER_DEATH_", StringComparison.OrdinalIgnoreCase) ||
            (riderSlot.StartsWith("RIDER_COMBAT_", StringComparison.OrdinalIgnoreCase) && riderSlot.Length > 13 && riderSlot[13..].All(char.IsDigit));

        // Moves the bones directly under animroot into the new animroot's space, keeping their world placement
        void Reparent(AnimationClip.KeyFrame frame, Matrix oldToNewAnimRootSpace)
        {
            foreach (var child in _riderSkeleton.GetDirectChildBones(0).Where(x => x < frame.Position.Count))
            {
                (LocalMatrix(frame, child) * oldToNewAnimRootSpace).Decompose(out _, out var rotation, out var translation);
                frame.Position[child] = translation;
                frame.Rotation[child] = Quaternion.Normalize(rotation);
            }
        }

        // Normalised: animroot rotations are 8-bit quantised, and off unit length they'd distort the matrix
        static Matrix LocalMatrix(AnimationClip.KeyFrame frame, int bone) =>
            Matrix.CreateFromQuaternion(Quaternion.Normalize(frame.Rotation[bone])) * Matrix.CreateTranslation(frame.Position[bone]);
    }
}
