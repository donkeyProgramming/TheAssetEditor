using System.Text;
using Editors.AnimationMeta.SuperView.Visualisation;
using Editors.AnimationMeta.SuperView.Visualisation.Instances;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.RigidModel.Transforms;
using Test.TestingUtility.Shared;

namespace Test.AnimationMeta
{
    // EFFECT tags in SuperView: particles on the tag's bone, on the main animation's clock from the tag's Start Time.
    public class VfxEffectTests
    {
        record Provider(GameSkeleton Skeleton) : ISkeletonProvider;

        // One emitter, 30 particles a second that live 10 s and do not move, carried by the bone.
        const string Smoke = """<shader_library file="fx/particle_vfx_library.hlsl"/><vfx revision="2"><emitters><emitter type="continuous_mesh" id="puff"><property name="InfiniteLife" quality="0" value="true"/><property name="ParticleSimulationSpace" quality="0" value="LOCAL_SPACE_POSITION_ROTATION"/><property name="EmissionRateCurve" quality="0">x_min,0,0;0.0,30.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,30.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1</property><property name="ParticleLifeTime" quality="0">x_min,0,0;0.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1</property></emitter></emitters><behaviour_chains><behaviour_chain id="puff"><quality value="0"><modifier fragment="ps_texture"><texture_property name="diffuse" type="file" path=""/></modifier></quality></behaviour_chain></behaviour_chains><effect_render_stacks><effect_render_stack id="puff"/></effect_render_stacks></vfx><vfx_references></vfx_references>""";

        GroupNode _root = null!;
        AnimationPlayer _player = null!;
        Provider _skeleton = null!;
        MetaDataBuilder _builder = null!;
        AssetEditorTestRunner _runner = null!;

        [TearDown]
        public void TearDown() => _builder.Dispose();

        [SetUp]
        public void Setup()
        {
            // PackFileService is internal to Shared.Core; the runner's is the public way to an in-memory pack.
            var runner = _runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var pack = runner.CreateOutputPack();
            runner.PackFileService.AddFilesToPack(pack, [new NewPackFileEntry("vfx", PackFile.CreateFromBytes("test_smoke.xml", Utf16(Smoke)))]);
            // Only the EFFECT path runs here, and it uses none of the mesh, render or skeleton loaders.
            _builder = new MetaDataBuilder(null!, null!, null!, runner.PackFileService, null!, null!);

            // Two bones; the fx bone walks 2 m/s along X, 1 m up, in 21 frames over 2 s so whole-frame times sample exactly.
            _player = new AnimationPlayer();
            var skeleton = new GameSkeleton(SkeletonFile(), _player);
            var clip = new AnimationClip { PlayTimeInSec = 2 };
            for (var frame = 0; frame <= 20; frame++)
            {
                clip.DynamicFrames.Add(new AnimationClip.KeyFrame
                {
                    Position = [Vector3.Zero, new Vector3(frame * 0.2f, 1, 0)],
                    Rotation = [Quaternion.Identity, Quaternion.Identity],
                    Scale = [Vector3.One, Vector3.One],
                });
            }
            _player.SetAnimation(clip, skeleton);
            _player.IsEnabled = true;
            _skeleton = new Provider(skeleton);
            _root = new GroupNode("root");
        }

        static AnimationFile SkeletonFile()
        {
            var file = new AnimationFile();
            file.Header.SkeletonName = "test_skeleton";
            file.Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = AnimationFile.BoneIndexNoParent },
                new AnimationFile.BoneInfo { Id = 1, Name = "fx", ParentId = 0 },
            ];
            var bind = new AnimationFile.Frame();
            bind.Transforms.Add(new RmvVector3(0, 0, 0));
            bind.Transforms.Add(new RmvVector3(0, 1, 0));
            bind.Quaternion.Add(new RmvVector4(0, 0, 0, 1));
            bind.Quaternion.Add(new RmvVector4(0, 0, 0, 1));
            var part = new AnimationFile.AnimationPart();
            part.DynamicFrames.Add(bind);
            file.AnimationParts.Add(part);
            return file;
        }

        static byte[] Utf16(string text) => [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)];

        static Effect_v11 Tag(string vfx, int bone = 1, float start = 0.5f, bool tracking = true) =>
            new() { VfxName = vfx, NodeIndex = bone, StartTime = start, EndTime = 0, Tracking = tracking };

        static ParsedMetadataFile Meta(params ParsedMetadataAttribute[] tags) => new() { Version = 2, Attributes = [.. tags] };

        List<IMetaDataInstance> Build(ParsedMetadataFile? persistent, ParsedMetadataFile meta) =>
            _builder.Create(persistent, meta, null, _root, _skeleton, _player, null!);

        static List<ParticleState> States(VfxEffectInstance instance) => instance.Simulator!.Emitters.SelectMany(ParticleSimulator.States).ToList();

        [Test]
        public void Effect_Spawns_OnItsBone_OnlyAfterItsStartTime()
        {
            var instance = (VfxEffectInstance)Build(null, Meta(Tag("test_smoke"))).Single();

            instance.Update(0.4f);
            Assert.That(States(instance), Is.Empty, "particles before the tag's Start Time");

            instance.Update(1.5f);
            var states = States(instance);
            Assert.That(states.Count, Is.EqualTo(30).Within(1), "one second of 30 a second");
            // At 1.5 s the bone is at (3, 1, 0); local-space particles ride on it.
            Assert.That(states.Max(x => Vector3.Distance(x.Position, new Vector3(3, 1, 0))), Is.LessThan(0.01f));
        }

        // The 2 s clip plays 1.5, 1.9, loops to 0.1, then 0.5: a persistent effect runs on across the loop (2.5 s of
        // particles), a tag in the animation's own meta restarts with it (0.5 s), and both ride the bone where the clip is now.
        [TestCase(true, 75)]
        [TestCase(false, 15)]
        public void PersistentEffect_RunsOnAcrossALoop(bool persistent, int particles)
        {
            var tag = Meta(Tag("test_smoke", start: 0));
            var instance = (VfxEffectInstance)(persistent ? Build(tag, Meta()) : Build(null, tag)).Single();
            foreach (var time in new[] { 1.5f, 1.9f, 0.1f, 0.5f })
                instance.Update(time);
            var states = States(instance);
            Assert.That(states.Count, Is.EqualTo(particles).Within(1));
            Assert.That(states.Max(x => Vector3.Distance(x.Position, new Vector3(1, 1, 0))), Is.LessThan(0.01f));
        }

        [Test]
        public void UntrackedEffect_StaysWhereItsBoneWasAtTheStart()
        {
            var instance = (VfxEffectInstance)Build(null, Meta(Tag("test_smoke", tracking: false))).Single();
            instance.Update(1.5f);
            // The bone was at (1, 1, 0) at the tag's 0.5 s start.
            Assert.That(States(instance).Max(x => Vector3.Distance(x.Position, new Vector3(1, 1, 0))), Is.LessThan(0.01f));
        }

        [Test]
        public void MissingEffect_KeepsItsLocator_AndDrawsNoParticles()
        {
            Assert.DoesNotThrow(() => Build(null, Meta(Tag("no_such_fx"))));
            Assert.That(_root.Children.Select(x => x.Name), Is.EquivalentTo(new[] { "Effect:no_such_fx" }));
        }

        [Test]
        public void ShowVfxOff_DrawsTheLocatorOnly()
        {
            _builder.ShowVfx = false;
            var instance = (VfxEffectInstance)Build(null, Meta(Tag("test_smoke"))).Single();
            Assert.That(instance.Simulator, Is.Null);
            Assert.That(_root.Children.OfType<VfxEffectNode>(), Is.Empty);
        }

        // A tag's id is the vfx_filter_id of the battle entities it plays on (battle_entities_tables): 0 plays on every entity,
        // any other id only on the entity with that filter. The Terracotta Sentinel's metas carry every effect twice, id 1 for
        // the unit and id 2 for its Regiment of Renown, and SuperView drew both. Unset, the filter is the lowest id the two
        // metas carry, the persistent one's included.
        [TestCase(null, new[] { "Effect:every_entity", "Effect:filter_1" })]
        [TestCase(2, new[] { "Effect:every_entity", "Effect:filter_2" })]
        public void TaggedEffect_PlaysOnlyOnItsVfxFilter(int? filter, string[] drawn)
        {
            static Effect_v11 Filtered(string vfx, int id)
            {
                var tag = Tag(vfx);
                tag.Id = id;
                return tag;
            }

            _builder.VfxFilterId = filter;
            Build(Meta(Filtered("filter_1", 1)), Meta(Filtered("every_entity", 0), Filtered("filter_2", 2)));
            Assert.That(_root.Children.Select(x => x.Name), Is.EquivalentTo(drawn));
        }

        [Test]
        public void DisablePersistentVfx_DropsThePersistentMetasEffects()
        {
            var persistent = Meta(Tag("test_smoke", start: 0));
            Build(persistent, Meta(new DisablePersistantVfx_v10()));
            Assert.That(_root.Children.OfType<VfxEffectNode>(), Is.Empty, "DISABLE_PERSISTENT_VFX kept the persistent meta's effect");
            Assert.That(Build(persistent, Meta()), Has.Count.EqualTo(1), "without it, the persistent effect is built");
        }

        // Every edit in the meta panel rebuilds every effect, so a rebuild reuses what it resolved of an effect's vfx file and
        // reads the file again only once Forget names a changed one, as SuperView does when a pack changes: smoke rewritten to
        // emit nothing shows only after.
        [Test]
        public void Rebuild_ReusesTheResolvedFile_UntilForgetNamesIt()
        {
            var tag = Meta(Tag("test_smoke"));
            int Particles()
            {
                var instance = (VfxEffectInstance)Build(null, tag).Single();
                instance.Update(1.5f);
                return States(instance).Count;
            }
            Particles();
            _runner.PackFileService.SaveFile(_runner.PackFileService.FindFile(@"vfx\test_smoke.xml")!, Utf16(Smoke.Replace("30.0", "0.0")));
            var reused = Particles();
            _builder.Forget([@"vfx\test_smoke.xml"]);
            var reread = Particles();
            Assert.Multiple(() =>
            {
                Assert.That(reused, Is.EqualTo(30).Within(1), "the rebuild read the file again");
                Assert.That(reread, Is.Zero, "Forget kept the old file");
            });
        }

        // ...and samples the bone afresh when the animation changes: rebuilt on a clip that holds the fx bone at (0, 1, 5), the
        // smoke rides there, not where the first clip had the bone at the same times; and an untracked effect, which keeps where
        // its bone was at the start, takes that from the new clip too.
        [TestCase(true)]
        [TestCase(false)]
        public void Rebuild_AfterTheClipChanges_RidesTheNewClip(bool tracking)
        {
            var tag = Meta(Tag("test_smoke", tracking: tracking));
            ((VfxEffectInstance)Build(null, tag).Single()).Update(1.5f);
            var held = new AnimationClip { PlayTimeInSec = 2 };
            for (var frame = 0; frame <= 20; frame++)
            {
                held.DynamicFrames.Add(new AnimationClip.KeyFrame
                {
                    Position = [Vector3.Zero, new Vector3(0, 1, 5)],
                    Rotation = [Quaternion.Identity, Quaternion.Identity],
                    Scale = [Vector3.One, Vector3.One],
                });
            }
            _player.SetAnimation(held, _skeleton.Skeleton);
            var instance = (VfxEffectInstance)Build(null, tag).Single();
            instance.Update(1.5f);
            Assert.That(States(instance).Max(x => Vector3.Distance(x.Position, new Vector3(0, 1, 5))), Is.LessThan(0.01f));
        }

        // A slider in the meta panel rebuilds on every step: the effect whose tag did not change keeps what was built for it,
        // particles and all, and only the one that changed is built again.
        [Test]
        public void Rebuild_KeepsTheEffectsWhoseTagsDidNotChange()
        {
            var still = Tag("test_smoke");
            var moved = Tag("test_smoke", start: 0.2f);
            var meta = Meta(still, moved);
            var first = Build(null, meta);
            first[0].Update(1.5f);
            moved.Position = new Vector3(0, 0, 1);
            var second = Build(null, meta);
            Assert.Multiple(() =>
            {
                Assert.That(second[0], Is.SameAs(first[0]), "the unchanged effect was built again");
                Assert.That(States((VfxEffectInstance)second[0]), Has.Count.EqualTo(30).Within(1), "and lost its particles");
                Assert.That(second[1], Is.Not.SameAs(first[1]), "the moved effect was kept");
            });
        }

        // ...but anything that changes what an effect draws builds it again: its colour when it is picked in the list, the
        // vfx toggle, another skeleton, or the caller having cleaned it up out of the scene (as a mesh change does).
        [TestCase("picked")]
        [TestCase("vfx off")]
        [TestCase("cleaned up")]
        [TestCase("skeleton swapped")]
        public void Rebuild_BuildsAnEffectAgain_When(string change)
        {
            var tag = Tag("test_smoke");
            var meta = Meta(tag);
            var first = Build(null, meta).Single();
            ParsedMetadataAttribute? picked = null;
            if (change == "picked")
                picked = tag;
            if (change == "vfx off")
                _builder.ShowVfx = false;
            if (change == "cleaned up")
                first.CleanUp();
            if (change == "skeleton swapped")
                _skeleton = new Provider(new GameSkeleton(SkeletonFile(), _player));
            var second = _builder.Create(null, meta, picked, _root, _skeleton, _player, null!).Single();
            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(_root.Children.Count(x => x.Name == "Effect:test_smoke"), Is.EqualTo(change == "cleaned up" ? 1 : 2));
            });
        }

        [Test]
        public void EffectOnABoneTheSkeletonLacks_DrawsAtTheOrigin()
        {
            var instance = (VfxEffectInstance)Build(null, Meta(Tag("test_smoke", bone: 99))).Single();
            Assert.DoesNotThrow(() => instance.Update(1.5f));
            Assert.That(States(instance).Max(x => x.Position.Length()), Is.LessThan(0.01f));
        }
    }
}
