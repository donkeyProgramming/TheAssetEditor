using AnimationEditor.MountAnimationCreator.Services;
using Microsoft.Xna.Framework;
using static Test.AnimationVisualEditors.VanillaRiders;

namespace Test.AnimationVisualEditors
{
    public class AttachedRiderRefitTests
    {
        [Test]
        public void StretchesAndRepeats_AndStaysAttached()
        {
            var pfs = VanillaRiders.Load().PackFileService;
            var rider = Skeleton(pfs, @"animations\skeletons\humanoid01.anim");
            var mount = Skeleton(pfs, @"animations\skeletons\horse01.anim");
            var riderRun = SlotClip(pfs, Set(pfs, AttachedSet), "RIDER_RUN_1", rider);
            var mountWalk = SlotClip(pfs, Set(pfs, MountSet), "WALK_1", mount);
            Assume.That(mountWalk.DynamicFrames.Count, Is.Not.EqualTo(riderRun.DynamicFrames.Count), "the two need different lengths");

            var none = new AttachedRiderRefit(false, 1, Vector3.Zero, Vector3.Zero);
            Assert.That(none.ChangesAnything(riderRun, mountWalk), Is.False);

            var stretch = new AttachedRiderRefit(true, 1, Vector3.Zero, Vector3.Zero);
            Assert.That(stretch.ChangesAnything(riderRun, mountWalk), Is.True);
            var stretched = stretch.Apply(riderRun, mountWalk, rider);
            Assert.That(stretched.DynamicFrames.Count, Is.EqualTo(mountWalk.DynamicFrames.Count));
            Assert.That(stretched.PlayTimeInSec, Is.EqualTo(mountWalk.PlayTimeInSec).Within(1e-3));
            Assert.That(RiderAttachmentConverter.IsAttached(stretched), Is.True);
            Assert.That(stretch.ChangesAnything(stretched, mountWalk), Is.False, "already fits");

            var twice = new AttachedRiderRefit(false, 2, Vector3.Zero, Vector3.Zero).Apply(riderRun, mountWalk, rider);
            Assert.That(twice.DynamicFrames.Count, Is.EqualTo(riderRun.DynamicFrames.Count * 2));
            Assert.That(twice.PlayTimeInSec, Is.EqualTo(riderRun.PlayTimeInSec * 2).Within(1e-3));

            // A rotation offset turns the rider around the attach bone
            var turned = new AttachedRiderRefit(false, 1, Vector3.Zero, new Vector3(90, 0, 0)).Apply(riderRun, mountWalk, rider);
            var pelvis = rider.GetBoneIndexByName("root");
            var before = World(rider, riderRun, 0, pelvis);
            var after = World(rider, turned, 0, pelvis);
            Assert.That(after.Length(), Is.EqualTo(before.Length()).Within(1e-4), "same distance from the attach bone");
            Assert.That(after.Y, Is.EqualTo(before.Y).Within(1e-4), "a yaw keeps the height");
            Assert.That(RiderAttachmentConverter.IsAttached(turned), Is.True);
        }
    }
}
