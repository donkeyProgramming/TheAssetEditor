using System;
using System.Linq;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;

namespace AnimationEditor.MountAnimationCreator.Services
{
    /// <summary>How an attached rider animation is fitted to a new mount's animation: an attached rider sits on the mount's
    /// attach bone, so it carries over as it is, unless it's repeated, stretched to the mount animation, or offset.</summary>
    public record AttachedRiderRefit(bool Stretch, int Plays, Vector3 Translation, Vector3 RotationDegrees)
    {
        Matrix Offset => Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(RotationDegrees.X), MathHelper.ToRadians(RotationDegrees.Y), MathHelper.ToRadians(RotationDegrees.Z))
            * Matrix.CreateTranslation(Translation);

        bool HasOffset => Translation != Vector3.Zero || RotationDegrees != Vector3.Zero;

        bool NeedsStretch(AnimationClip rider, AnimationClip mount) =>
            Stretch && mount.DynamicFrames.Count > 1 && (rider.DynamicFrames.Count * Math.Max(Plays, 1) != mount.DynamicFrames.Count || Math.Abs(rider.PlayTimeInSec * Math.Max(Plays, 1) - mount.PlayTimeInSec) > 1e-3f);

        /// <summary>False when the rider animation would come out as it went in.</summary>
        public bool ChangesAnything(AnimationClip rider, AnimationClip mount) => Plays > 1 || HasOffset || NeedsStretch(rider, mount);

        public AnimationClip Apply(AnimationClip rider, AnimationClip mount, GameSkeleton riderSkeleton)
        {
            var output = rider.Clone();
            if (Plays > 1)
            {
                GameWorld.Core.Animation.AnimationEditor.LoopAnimation(output, Plays);
                output.PlayTimeInSec = rider.PlayTimeInSec * Plays;
            }

            if (NeedsStretch(rider, mount))
                output = GameWorld.Core.Animation.AnimationEditor.ReSample(riderSkeleton, output, mount.DynamicFrames.Count, mount.PlayTimeInSec);

            // The bones under animroot move with the offset; animroot stays on the attach bone
            if (HasOffset)
            {
                var offset = Offset;
                var children = riderSkeleton.GetDirectChildBones(0);
                foreach (var frame in output.DynamicFrames)
                {
                    foreach (var child in children.Where(x => x < frame.Position.Count))
                    {
                        var local = Matrix.CreateFromQuaternion(Quaternion.Normalize(frame.Rotation[child])) * Matrix.CreateTranslation(frame.Position[child]);
                        (local * offset).Decompose(out _, out var rotation, out var translation);
                        frame.Position[child] = translation;
                        frame.Rotation[child] = Quaternion.Normalize(rotation);
                    }
                }
            }
            return output;
        }
    }
}
