using System;
using System.Linq;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;

namespace AnimationEditor.MountAnimationCreator.Services
{
    /// <summary>
    /// Converts a rider animation between the two ways WH3 puts a rider on a mount.
    /// Synchronized: rider and mount share the ground frame; the rider's animroot moves with the
    /// mount's and the rider sits at saddle height (e.g. hu1_empire_hr1_warhorse_hammer).
    /// Attached: the game glues the rider's animroot to an ap_ bone of the mount's tech skeleton
    /// (e.g. ap_riderposition_0 in emp_warbeast_horse_01's tech skeleton), so the animroot stays at
    /// the origin and the rider sits on it (e.g. hu1_wh3_kislev_hr1_warhorse_sword).
    /// Both directions need the mount animation the rider plays with: the attach bone moves with it.
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

            // The attach bone isn't animated: it's its rest transform under its parent, which the
            // mount animation moves. The parent has to be the same bone in the mount skeleton.
            _attachParentBone = attachSkeleton.GetParentBoneIndex(attachBone);
            if (_attachParentBone < 0 || _attachParentBone >= mountSkeleton.BoneCount ||
                !string.Equals(attachSkeleton.BoneNames[_attachParentBone], mountSkeleton.BoneNames[_attachParentBone], StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The attach bone '{attachSkeleton.BoneNames[attachBone]}' hangs off a bone the mount skeleton '{mountSkeleton.SkeletonName}' doesn't have at the same index.");

            // Normalised: vanilla tech skeletons store it slightly off unit length (emp_warbeast_horse_01: |q|² ≈ 0.9997).
            _attachLocal = Matrix.CreateFromQuaternion(Quaternion.Normalize(attachSkeleton.Rotation[attachBone])) * Matrix.CreateTranslation(attachSkeleton.Translation[attachBone]);
        }

        /// <summary>The attach bone's world transform at <paramref name="riderFrame"/> of a rider clip with
        /// <paramref name="riderFrameCount"/> frames. The mount is sampled at the same point in time, so
        /// clips of different lengths line up start to end.</summary>
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

        /// <summary>Synchronized to attached: the rider keeps its pose relative to the attach bone and
        /// its animroot becomes the identity, as in vanilla attached animations.</summary>
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

        /// <summary>Attached to synchronized: the rider ends up where the attach bone puts it in the
        /// mount's ground frame. Its new animroot follows the mount's animroot, offset to sit under
        /// the attach point, which is how vanilla synchronized riders carry their locomotion.</summary>
        public AnimationClip ToSynchronized(AnimationClip attached)
        {
            var output = attached.Clone();
            var frameCount = output.DynamicFrames.Count;

            // The attach point's ground position relative to the mount's animroot, taken at the start.
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

        // Moves every bone directly under animroot (the pelvis "root" and the be_prop_ bones) from the
        // old animroot's space into the new one's, keeping where they end up in the world.
        void Reparent(AnimationClip.KeyFrame frame, Matrix oldToNewAnimRootSpace)
        {
            foreach (var child in _riderSkeleton.GetDirectChildBones(0).Where(x => x < frame.Position.Count))
            {
                (LocalMatrix(frame, child) * oldToNewAnimRootSpace).Decompose(out _, out var rotation, out var translation);
                frame.Position[child] = translation;
                frame.Rotation[child] = Quaternion.Normalize(rotation);
            }
        }

        // Normalised: animroot rotations are stored 8-bit quantised (|q|² is up to ~0.7% off in vanilla
        // turns), which as a matrix is a small distortion no stored pose can keep.
        static Matrix LocalMatrix(AnimationClip.KeyFrame frame, int bone) =>
            Matrix.CreateFromQuaternion(Quaternion.Normalize(frame.Rotation[bone])) * Matrix.CreateTranslation(frame.Position[bone]);
    }
}
