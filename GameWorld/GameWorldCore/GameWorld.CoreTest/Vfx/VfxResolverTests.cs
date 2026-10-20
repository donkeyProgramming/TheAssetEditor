using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static GameWorld.Core.Test.Vfx.VfxTestFiles;

namespace GameWorld.Core.Test.Vfx
{
    public class VfxResolverTests
    {
        static VfxResolveResult Resolve(IPackFileService pack, string name) => new VfxResolver(pack).Resolve(name, new Random(1));

        [Test]
        public void Unsaved_bytes_stand_in_for_the_packs_copy()
        {
            var packs = VfxTestFiles.Pack(("a.xml", VfxTestFiles.Xml(emitters: VfxTestFiles.Emitter("saved"))));
            var unsaved = new Dictionary<string, byte[]> { [VfxResolver.Key("a")] = VfxTestFiles.Xml(emitters: VfxTestFiles.Emitter("edited")) };
            var result = new VfxResolver(packs, unsaved).Resolve("a", new Random(1));
            Assert.That(result.Emitters.Select(e => e.Emitter.Id), Is.EqualTo(new[] { "edited" }));
        }

        [Test]
        public void NestedReferences_ComposeTransforms_AndTheOuterOverrideWins()
        {
            var pack = Pack(
                ("lib.xml", Xml(emitters: Emitter("a") + Emitter("b"))),
                ("mid.xml", Xml(references: Reference("lib", posX: "1.00", overrides: Override("a", tint: "0.50") + Override("b", enable: false)))),
                ("top.xml", Xml(references: Reference("mid", posY: "2.00", rotY: "90.00", overrides: Override("a", tint: "0.25")))));

            var result = Resolve(pack, "top");

            var a = result.Emitters.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Problems, Is.Empty);
                Assert.That(a.Emitter.Id, Is.EqualTo("a"), "b is switched off by mid");
                Assert.That(a.Override.TintMin.X, Is.EqualTo(0.25f), "top's override, not mid's");
                // lib sits 1 m along X inside mid; mid is turned 90 degrees about Y and raised 2 m inside top.
                Assert.That(Vector3.Distance(Vector3.Transform(Vector3.Zero, a.ReferenceTransform), new Vector3(0, 2, -1)), Is.LessThan(1e-4f));
            });
        }

        // A file that cannot be read (a pack rebuilt on disk while the editor has it open) threw out of Resolve,
        // through MetaDataBuilder.Create, and wiped every tag in SuperView.
        [Test]
        public void UnreadableFile_IsAProblem_NotAThrow()
        {
            var path = Path.Combine(Path.GetTempPath(), $"vfx_test_gone_{Guid.NewGuid()}.xml");
            File.WriteAllBytes(path, Xml(emitters: Emitter("a")));
            var pack = Pack([PackFile.CreateFromFileSystem("gone.xml", path)]);
            File.Delete(path);
            VfxResolveResult? result = null;
            Assert.That(() => result = Resolve(pack, "gone"), Throws.Nothing);
            Assert.That(result!.Problems, Has.One.Matches<string>(x => x.Contains("gone.xml")));
        }

        // A particle event's effect resolves as a top-level one would, into the event. One missing says so, and one already on
        // the way to it (an effect whose particles start itself) is not followed again.
        [Test]
        public void ParticleEvents_ResolveTheirEffects_AndAreNotFollowedRoundACycle()
        {
            var events = """<property_event name="OnParticleDeath" quality="0" vfx_name="boom" death_event_time_offset="0.00"/><property_event name="OnParticleSpawn" quality="0" vfx_name="fx" track_position="true" spawn_event_time_offset="0.00"/>""";
            var pack = Pack(("fx.xml", Xml(emitters: Emitter("e", events))), ("boom.xml", Xml(emitters: Emitter("b"))));
            var result = Resolve(pack, "fx");
            var e = result.Emitters.Single();
            Assert.Multiple(() =>
            {
                Assert.That(e.Events!.Select(x => (x.Event.Effect, x.Emitters.Select(c => c.Emitter.Id).Single())), Is.EqualTo(new[] { ("boom", "b") }));
                Assert.That(result.Problems, Has.One.Matches<string>(x => x.Contains("cycle") && x.Contains("fx")));
            });
            var missing = Resolve(Pack(("fx.xml", Xml(emitters: Emitter("e", events.Replace("boom", "gone"))))), "fx");
            Assert.That(missing.Problems, Has.One.Matches<string>(x => x.Contains("gone.xml")));
        }

        [Test]
        public void ReferenceCycle_IsSkippedAndReported_NotFollowedForever()
        {
            var pack = Pack(
                ("loop_a.xml", Xml(emitters: Emitter("in_a"), references: Reference("loop_b"))),
                ("loop_b.xml", Xml(references: Reference("loop_a"))));
            var result = Resolve(pack, "loop_a");
            Assert.That(result.Emitters.Select(x => x.Emitter.Id), Is.EqualTo(new[] { "in_a" }));
            Assert.That(result.Problems, Has.One.Matches<string>(x => x.Contains("cycle")));
        }

        [Test]
        public void MissingReference_SkipsThatBranch_AndKeepsTheRest()
        {
            var result = Resolve(Pack(("top.xml", Xml(emitters: Emitter("own"), references: Reference("nowhere")))), "top");
            Assert.That(result.Emitters.Select(x => x.Emitter.Id), Is.EqualTo(new[] { "own" }));
            Assert.That(result.Problems, Has.One.Matches<string>(x => x.Contains("vfx/nowhere.xml")));
        }

        [Test]
        public void MissingEffect_IsNotFound() => Assert.That(Resolve(Pack(), "nowhere").Found, Is.False);

        [TestCase("Top")]
        [TestCase("top.xml")]
        [TestCase(" top ")]
        public void TagName_FindsTheFile_WhateverItsCaseOrExtension(string name)
        {
            var result = Resolve(Pack(("top.xml", Xml(emitters: Emitter("own")))), name);
            Assert.That(result.Found, Is.True);
        }

        [Test]
        public void EmitterSwitchedOffAtUltra_IsDropped()
        {
            var off = Emitter("off", """<property name="Enable" quality="0" value="false"/>""");
            Assert.That(Resolve(Pack(("fx.xml", Xml(emitters: off))), "fx").Emitters, Is.Empty);
        }

        // A reference plays only the emitters it names: CA's editor lists every emitter its library had when the effect was
        // made, and one the library gained later is not played. wh3_dlc23_entity_drazhoath_axe_fire_trail reaches a library
        // whose flamethrower_continuous2 it never lists, and played, a weapon's fire trail threw flame 40 m. 1,824 of
        // vanilla's 11,555 references leave some emitter unnamed, in 1,311 effects. The one that names none points at a file
        // with no emitters of its own, so a reference naming none still plays them all.
        [Test]
        public void EmitterTheReferenceDoesNotName_IsNotPlayed()
        {
            var lib = ("lib.xml", Xml(emitters: Emitter("named") + Emitter("added_later")));
            var named = Resolve(Pack(lib, ("top.xml", Xml(references: Reference("lib", overrides: Override("named"))))), "top");
            var none = Resolve(Pack(lib, ("top.xml", Xml(references: Reference("lib")))), "top");
            Assert.Multiple(() =>
            {
                Assert.That(named.Emitters.Select(x => x.Emitter.Id), Is.EqualTo(new[] { "named" }));
                Assert.That(none.Emitters.Select(x => x.Emitter.Id), Is.EqualTo(new[] { "named", "added_later" }));
            });
        }

        // An override reaches an emitter anywhere under its reference; one naming an id nothing there has does nothing,
        // so it is said. A tracer switched on _rifle_100ms_aftertrail, which wh_main_lib_trail_small_arm does not contain.
        [Test]
        public void OverrideOfAnEmitterNothingBelowHas_IsAProblem()
        {
            var result = Resolve(Pack(
                ("lib.xml", Xml(emitters: Emitter("a"))),
                ("mid.xml", Xml(references: Reference("lib"))),
                ("top.xml", Xml(references: Reference("mid", overrides: Override("a") + Override("ghost", enable: false))))), "top");
            Assert.That((result.Emitters.Count, result.Problems), Is.EqualTo((1, new List<string> { "top: mid has no emitter ghost, so its override does nothing" })));
        }
    }
}
