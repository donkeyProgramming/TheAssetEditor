using System.IO;
using System.Linq;
using Shared.ByteParsing;
using Shared.GameFormats.AnimationPack;

namespace Test.CampaignAnimationSetEditor
{
    /// <summary>Round-trip tests for the binary campaign animation set format, using real vanilla
    /// WH3 files as fixtures. This format's load/write code previously had no test coverage.</summary>
    [TestFixture]
    public class CampaignAnimationBinRoundTripTests
    {
        static byte[] LoadFixture(string name) => File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));

        [TestCase("cam_bi2_dlc27_preyton_boss.bin")]
        [TestCase("cam_hu6_gor_axe.bin")]
        public void Load_then_write_then_reload_preserves_structure(string fixtureName)
        {
            var originalBytes = LoadFixture(fixtureName);
            var bin = CampaignAnimationBinLoader.Load(new ByteChunk(originalBytes));

            var reference = Path.GetFileNameWithoutExtension(fixtureName);
            var writtenBytes = CampaignAnimationBinLoader.Write(bin, reference);
            var reloaded = CampaignAnimationBinLoader.Load(new ByteChunk(writtenBytes));

            Assert.That(reloaded.Version, Is.EqualTo(bin.Version));
            Assert.That(reloaded.Reference, Is.EqualTo(reference));
            Assert.That(reloaded.SkeletonName, Is.EqualTo(bin.SkeletonName));
            Assert.That(reloaded.Status, Has.Count.EqualTo(bin.Status.Count));

            for (var i = 0; i < bin.Status.Count; i++)
            {
                Assert.That(reloaded.Status[i].Name, Is.EqualTo(bin.Status[i].Name), $"status[{i}].Name");

                if (bin.Status[i].Name == "global")
                {
                    Assert.That(reloaded.Status[i].Docks?.Count ?? 0, Is.EqualTo(bin.Status[i].Docks?.Count ?? 0), $"status[{i}].Docks");
                    Assert.That(reloaded.Status[i].Poses?.Count ?? 0, Is.EqualTo(bin.Status[i].Poses?.Count ?? 0), $"status[{i}].Poses");
                }
                else
                {
                    Assert.That(reloaded.Status[i].Idle?.Count ?? 0, Is.EqualTo(bin.Status[i].Idle?.Count ?? 0), $"status[{i}].Idle");
                    Assert.That(reloaded.Status[i].Locomotion?.Count ?? 0, Is.EqualTo(bin.Status[i].Locomotion?.Count ?? 0), $"status[{i}].Locomotion");
                    Assert.That(reloaded.Status[i].Action?.Count ?? 0, Is.EqualTo(bin.Status[i].Action?.Count ?? 0), $"status[{i}].Action");
                }
            }
        }

        [Test]
        public void Preyton_fixture_has_the_expected_status_normal_locomotion_entries()
        {
            var bin = CampaignAnimationBinLoader.Load(new ByteChunk(LoadFixture("cam_bi2_dlc27_preyton_boss.bin")));

            var statusNormal = bin.Status.Single(x => x.Name == "status_normal");
            Assert.That(statusNormal.Locomotion, Has.Count.EqualTo(2));
            Assert.That(statusNormal.Locomotion!.Select(x => x.Animation), Has.All.Contains("bird02"));
        }

        [Test]
        public void Gor_axe_fixture_has_a_global_status_with_docks()
        {
            var bin = CampaignAnimationBinLoader.Load(new ByteChunk(LoadFixture("cam_hu6_gor_axe.bin")));

            var global = bin.Status.SingleOrDefault(x => x.Name == "global");
            Assert.That(global, Is.Not.Null);
            Assert.That(global!.Docks, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>Regression coverage for a field-order bug in the original parser: PersistentMeta,
        /// PersistentMeta_Pose/_Dock, LocomotionEntry, TransitionEntry and UnknownEntry all read
        /// (Animation, AnimationMeta, SoundMeta, Type, ...) when the on-disk order is (Animation,
        /// Type, AnimationMeta, SoundMeta, ...), so meta paths landed in the wrong properties.
        /// PersistentMeta_Pose/_Dock also had no real "Weight" field - it was the 4th string
        /// (SoundMeta) misread as a float.</summary>
        [Test]
        public void Gor_axe_fixture_global_entries_decode_with_correct_field_order()
        {
            var bin = CampaignAnimationBinLoader.Load(new ByteChunk(LoadFixture("cam_hu6_gor_axe.bin")));
            var global = bin.Status.Single(x => x.Name == "global");

            var meta = global.PersitantMetaData!.Single();
            Assert.That(meta.Type, Is.EqualTo("global"));
            Assert.That(meta.AnimationMeta, Does.EndWith(".anm.meta"));

            var pose = global.Poses!.Single();
            Assert.That(pose.Type, Is.EqualTo("global"));
            Assert.That(pose.PoseId, Is.EqualTo(3));

            foreach (var dock in global.Docks!)
            {
                Assert.That(dock.Type, Is.EqualTo("global"));
                Assert.That(dock.Dock, Does.StartWith("DOCK_EQPT_"));
            }
        }

        [Test]
        public void Preyton_fixture_locomotion_and_action_entries_decode_with_correct_field_order()
        {
            var bin = CampaignAnimationBinLoader.Load(new ByteChunk(LoadFixture("cam_bi2_dlc27_preyton_boss.bin")));
            var statusNormal = bin.Status.Single(x => x.Name == "status_normal");

            foreach (var locomotion in statusNormal.Locomotion!)
            {
                Assert.That(locomotion.Type, Is.EqualTo("status_normal"));
                Assert.That(locomotion.SoundMeta, Is.Empty.Or.EndsWith(".snd.meta"));
            }

            var statusBattle = bin.Status.Single(x => x.Name == "status_battle");
            foreach (var action in statusBattle.Action!)
                Assert.That(action.Type, Is.EqualTo("status_normal"));
        }
    }
}
