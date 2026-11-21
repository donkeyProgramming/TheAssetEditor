using AnimationEditor.MountAnimationCreator.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using static Test.AnimationVisualEditors.VanillaRiders;

namespace Test.AnimationVisualEditors
{
    // The same performance can't be compared bone for bone across the two vanilla riders (they're different
    // animations), so they're compared on where the pelvis sits; a round trip is compared exactly.
    public class RiderAttachmentConverterTests
    {
        // Slots where the two vanilla riders do the same thing, so their poses are comparable
        static readonly string[] Locomotion = ["STAND", "WALK_1", "RUN_1", "CHARGE", "TURN_LEFT_90"];
        static readonly string[] Slots = [.. Locomotion, "ATTACK_1", "COMBAT_IDLE_1"];

        record Pair(IPackFileService Pfs, GameSkeleton Rider, GameSkeleton Mount, AnimationClip MountClip, AnimationClip Attached, AnimationClip Synchronized, RiderAttachmentConverter Converter);

        static Pair Load(string slot)
        {
            var pfs = VanillaRiders.Load().PackFileService;
            var rider = Skeleton(pfs, @"animations\skeletons\humanoid01.anim");
            var mount = Skeleton(pfs, @"animations\skeletons\horse01.anim");
            var tech = Skeleton(pfs, TechSkeleton);
            var mountClip = SlotClip(pfs, Set(pfs, MountSet), slot, mount);
            var attached = SlotClip(pfs, Set(pfs, AttachedSet), "RIDER_" + slot, rider);
            var synchronized = SlotClip(pfs, Set(pfs, SynchronizedSet), "RIDER_" + slot, rider);
            return new Pair(pfs, rider, mount, mountClip, attached, synchronized, new RiderAttachmentConverter(rider, mount, mountClip, tech, tech.GetBoneIndexByName(AttachBone)));
        }

        [TestCaseSource(nameof(Locomotion))]
        public void AttachedToSynchronized_SitsWhereTheVanillaSynchronizedRiderSits(string slot)
        {
            var p = Load(slot);
            var converted = p.Converter.ToSynchronized(p.Attached);
            var pelvis = p.Rider.GetBoneIndexByName("root");

            var frames = Math.Min(converted.DynamicFrames.Count, p.Synchronized.DynamicFrames.Count);
            for (var f = 0; f < frames; f++)
            {
                Assert.That((World(p.Rider, converted, f, pelvis) - World(p.Rider, p.Synchronized, f, pelvis)).Length(), Is.LessThan(0.1f), $"pelvis, frame {f}");
                Assert.That(World(p.Rider, converted, f, 0).Y, Is.EqualTo(0).Within(1e-4), $"animroot on the ground, frame {f}");
            }
            Assert.That(RiderAttachmentConverter.IsAttached(converted), Is.False);
        }

        [TestCaseSource(nameof(Locomotion))]
        public void SynchronizedToAttached_SitsWhereTheVanillaAttachedRiderSits(string slot)
        {
            var p = Load(slot);
            var converted = p.Converter.ToAttached(p.Synchronized);
            var pelvis = p.Rider.GetBoneIndexByName("root");

            var frames = Math.Min(converted.DynamicFrames.Count, p.Attached.DynamicFrames.Count);
            for (var f = 0; f < frames; f++)
                Assert.That((World(p.Rider, converted, f, pelvis) - World(p.Rider, p.Attached, f, pelvis)).Length(), Is.LessThan(0.1f), $"pelvis, frame {f}");
            Assert.That(RiderAttachmentConverter.IsAttached(converted), Is.True);
        }

        // Every bone but animroot comes back. Animroot can't after synchronized -> attached: an attached animation keeps
        // it at the origin, so vanilla's own synchronized animroot motion (it wanders in turns, combat idles and deaths)
        // has nowhere to go. The rebuilt one follows the mount's, which leaves every other bone where it was.
        [TestCaseSource(nameof(Slots))]
        public void RoundTrip_KeepsEveryBoneButAnimroot(string slot)
        {
            var p = Load(slot);
            foreach (var (original, back) in new[] {
                (p.Attached, p.Converter.ToAttached(p.Converter.ToSynchronized(p.Attached))),
                (p.Synchronized, p.Converter.ToSynchronized(p.Converter.ToAttached(p.Synchronized))) })
            {
                // Compared with the input's animroot and its children normalised: vanilla stores them slightly off unit length
                // (8-bit quantised), which no rewritten pose keeps.
                var normalised = original.Clone();
                foreach (var frame in normalised.DynamicFrames)
                    foreach (var bone in p.Rider.GetDirectChildBones(0).Append(0))
                        frame.Rotation[bone] = Quaternion.Normalize(frame.Rotation[bone]);

                for (var f = 0; f < normalised.DynamicFrames.Count; f++)
                    for (var bone = 1; bone < p.Rider.BoneCount; bone++)
                        Assert.That((World(p.Rider, normalised, f, bone) - World(p.Rider, back, f, bone)).Length(), Is.LessThan(1e-4f), $"{p.Rider.BoneNames[bone]}, frame {f}");
            }
        }

        [Test]
        public void IsAttached_TellsTheVanillaSetsApart()
        {
            var p = Load("STAND");
            Assert.That(RiderAttachmentConverter.IsAttached(p.Attached), Is.True);
            Assert.That(RiderAttachmentConverter.IsAttached(p.Synchronized), Is.False);
        }

        [TestCase("RIDER_DEATH_STAND_1", true)]
        [TestCase("RIDER_COMBAT_12", true)]
        [TestCase("RIDER_COMBAT_IDLE_1", false)]
        [TestCase("RIDER_COMBAT_READY", false)]
        [TestCase("RIDER_RUN_1", false)]
        public void StaysSynchronized(string slot, bool expected) => Assert.That(RiderAttachmentConverter.StaysSynchronized(slot), Is.EqualTo(expected));
    }
}
