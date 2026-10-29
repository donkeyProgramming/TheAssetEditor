using System.Text;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Test.Vfx
{
    public class VfxFileTests
    {
        [Test]
        public void A_tier_takes_its_override_and_the_base_where_it_has_none()
        {
            var bytes = VfxTestFiles.Xml(
                emitters: VfxTestFiles.Emitter("e", """<property name="A" quality="0" value="1"/><property name="A" quality="1" value="2"/><property name="B" quality="0" value="3"/><property name="C" quality="2" value="4"/>"""),
                chains: """<behaviour_chain id="e"><quality value="0"><modifier fragment="ps_texture"/></quality><quality value="1"><modifier fragment="vs_colour"/></quality></behaviour_chain>""");
            var high = VfxFile.Parse(bytes, tier: 1);
            var properties = high.Emitters[0].Properties;
            Assert.That((properties["A"].Value, properties["B"].Value, properties.ContainsKey("C")), Is.EqualTo(("2", "3", false)));
            Assert.That(high.Chains["e"].Modifiers.Single().Fragment, Is.EqualTo("vs_colour"));
            Assert.That(VfxFile.Parse(bytes, tier: 2).Chains.ContainsKey("e"), Is.False);   // no block 2: draws nothing at Medium
            Assert.That(VfxFile.Parse(bytes).Emitters[0].Properties["A"].Value, Is.EqualTo("1"));
        }

        // continuous_rigid particles are meshes: RigidData lists the models, one picked per particle; an entry naming none is dropped.
        [Test]
        public void Rigid_data_lists_each_model_with_its_pivot_and_animation()
        {
            var rigid = """<property_rigid_data name="RigidData" quality="0"><rigid_entry><ModelFile value="vfx/models/rock.wsmodel"/><PivotPoint x="0.50" y="0.25" z="0.50"/><AnimFile value=""/><AnimSpeed value="1.00"/></rigid_entry><rigid_entry><ModelFile value="vfx/models/rat.wsmodel"/><PivotPoint x="0.50" y="0.50" z="0.50"/><AnimFile value="animations/rat.anim"/><AnimSpeed value="2.50"/></rigid_entry><rigid_entry><ModelFile value=""/><PivotPoint x="0.50" y="0.50" z="0.50"/><AnimFile value=""/><AnimSpeed value="1.00"/></rigid_entry></property_rigid_data>""";
            var emitter = VfxFile.Parse(VfxTestFiles.Xml(emitters: VfxTestFiles.Emitter("e", rigid))).Emitters[0];
            Assert.That(emitter.Rigid, Is.EqualTo(new[]
            {
                new VfxRigidEntry("vfx/models/rock.wsmodel", new Vector3(0.5f, 0.25f, 0.5f), null),
                new VfxRigidEntry("vfx/models/rat.wsmodel", new Vector3(0.5f), "animations/rat.anim", 2.5f),
            }));
        }

        // property_event: OnParticleSpawn and OnParticleDeath name an effect and what of the particle it follows; one naming none
        // ("" on 80 vanilla events) starts nothing.
        [Test]
        public void Particle_events_name_an_effect_and_what_it_follows()
        {
            var events = """<property_event name="OnParticleSpawn" quality="0" vfx_name="trail" track_position="true" track_scale="false" track_colour="true" track_alpha="false" track_lifetime="true" spawn_event_time_offset="-0.30" track_orientation_mode="orientation_by_velocity"/><property_event name="OnParticleDeath" quality="0" vfx_name="" death_event_time_offset="0.00"/>""";
            var emitter = VfxFile.Parse(VfxTestFiles.Xml(emitters: VfxTestFiles.Emitter("e", events))).Emitters[0];
            Assert.That(emitter.Events, Is.EqualTo(new[] { new VfxParticleEvent(false, "trail", true, false, true, false, true, VfxEventTurn.Velocity, -0.3f) }));
        }

        [TestCase("")]
        [TestCase("0")]
        public void An_event_naming_no_effect_starts_nothing(string name)
        {
            var events = $"""<property_event name="OnParticleSpawn" quality="0" vfx_name="{name}"/>""";
            Assert.That(VfxFile.Parse(VfxTestFiles.Xml(emitters: VfxTestFiles.Emitter("e", events))).Emitters[0].Events, Is.Empty);
        }

        // ref_spawn_speed="-1.00" is CA's unset; 0 is a speed.
        [TestCase("", null)]
        [TestCase(" ref_spawn_speed=\"-1.00\"", null)]
        [TestCase(" ref_spawn_speed=\"0.00\"", 0f)]
        [TestCase(" ref_spawn_speed=\"30.00\"", 30f)]
        public void Ref_spawn_speed_of_minus_one_is_unset(string root, float? expected) =>
            Assert.That(VfxFile.Parse(VfxTestFiles.Xml(root: root)).RefSpawnSpeed, Is.EqualTo(expected));

        // No vanilla chain lacks a value="0" block; one a modder wrote without it draws nothing at Ultra in game.
        [Test]
        public void Ultra_reads_no_other_tiers_block()
        {
            var bytes = VfxTestFiles.Xml(
                emitters: VfxTestFiles.Emitter("e", """<property name="A" quality="0" value="1"/>"""),
                chains: """<behaviour_chain id="e"><quality value="1"><modifier fragment="vs_colour"/></quality></behaviour_chain>""");
            Assert.That(VfxFile.Parse(bytes).Chains.ContainsKey("e"), Is.False);
        }

        const string Sample = """
            <shader_library file="fx/particle_vfx_library.hlsl"/><vfx revision="2" construction_type="1" enable_in_ted="false">
            <emitters>
            <emitter type="continuous_mesh" id="smoke">
            <property name="Enable" quality="2" value="false"/>
            <property name="CullDistances" quality="0" x="0.00" y="0.00" z="700.00" w="700.00"/>
            <property name="EmissionRateCurve" quality="0">x_min,0,0;0.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1</property>
            <property name="EmissionRateCurve" quality="1">x_min,0,0;0.0,5.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,5.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1</property>
            <ranged_property name="LifeTime" quality="0" min="1.00" max="2.00"/>
            <property name="ParticleSimulationSpace" quality="0" value="LOCAL_SPACE_POSITION_ROTATION"/>
            </emitter>
            </emitters>
            <behaviour_chains>
            <behaviour_chain id="smoke">
            <quality value="1">
            <modifier fragment="ps_texture"><texture_property name="diffuse" type="file" path="vfx\textures\low.dds"/></modifier>
            </quality>
            <quality value="0">
            <modifier fragment="ps_texture">
            <texture_property name="diffuse" type="file" path="vfx\textures\smoke_dense_loop_diffuse.dds"/>
            <property name="pre_multiplied_alpha" type="bool" value="true"/>
            </modifier>
            <modifier fragment="vs_animated_texture"><property name="no_cells_xy" type="float2" value="float2(8.0,4.0)"/></modifier>
            </quality>
            </behaviour_chain>
            </behaviour_chains>
            <effect_render_stacks>
            <effect_render_stack id="smoke"/>
            <effect_render_stack id="ripple"><property name="scene_view_type" quality="0" value="DISTORTION"/></effect_render_stack>
            </effect_render_stacks>
            </vfx><vfx_references>
            <vfx inst_name="xref" vfx_ref="wh3_dlc23_lib_warmachines" notes="" scale_min="1.00" scale_max="2.00" pos_x="0.50" pos_y="0.00" pos_z="0.00" rot_x="0.00" rot_y="90.00" rot_z="0.00" start_time_min="-1.00" start_time_max="0.00" infinite_life="true" simulation_speed_multiplier="0.50">
            <emitter name="furnace_fire" enable="true" tint_min_r="0.50" tint_min_g="1.00" tint_min_b="1.00" tint_min_a="1.00" tint_max_r="1.00" tint_max_g="1.00" tint_max_b="1.00" tint_max_a="1.00" particle_lifetime_bias_min="0.10" particle_lifetime_bias_max="0.20" emitter_lifetime_bias_min="0.00" emitter_lifetime_bias_max="0.00" spawn_rate_scale_factor_min="2.00" spawn_rate_scale_factor_max="2.00" sound_record=""/>
            <emitter name="base_red_smoke" enable="false" tint_min_r="1.00" tint_min_g="1.00" tint_min_b="1.00" tint_min_a="1.00" tint_max_r="1.00" tint_max_g="1.00" tint_max_b="1.00" tint_max_a="1.00" particle_lifetime_bias_min="0.00" particle_lifetime_bias_max="0.00" emitter_lifetime_bias_min="0.00" emitter_lifetime_bias_max="0.00" spawn_rate_scale_factor_min="1.00" spawn_rate_scale_factor_max="1.00" sound_record=""/>
            </vfx>
            </vfx_references><!-- EDITOR DATA--><editor_data><effect_components></effect_components></editor_data>
            """;

        // CA writes these files as UTF-16 with a byte order mark.
        static VfxFile Parse(string text) => VfxFile.Parse([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)]);

        [Test]
        public void Emitter_ReadsQualityZeroOnly_InEveryValueShape()
        {
            var emitter = Parse(Sample).Emitters.Single();
            Assert.Multiple(() =>
            {
                Assert.That((emitter.Id, emitter.Type), Is.EqualTo(("smoke", "continuous_mesh")));
                Assert.That(emitter.Properties.ContainsKey("Enable"), Is.False, "a quality-2-only property is not Ultra's");
                Assert.That(emitter.Properties["CullDistances"].Number("z"), Is.EqualTo(700));
                Assert.That(VfxCurve.Parse(emitter.Properties["EmissionRateCurve"].Text).Evaluate(0, 0, 0), Is.EqualTo(10), "quality 0's curve, not quality 1's");
                Assert.That((emitter.Properties["LifeTime"].Number("min"), emitter.Properties["LifeTime"].Number("max")), Is.EqualTo((1f, 2f)));
                Assert.That(emitter.Properties["ParticleSimulationSpace"].Value, Is.EqualTo("LOCAL_SPACE_POSITION_ROTATION"));
            });
        }

        [Test]
        public void Chain_ReadsTheQualityZeroBlock_WhereverItSits()
        {
            var chain = Parse(Sample).Chains["smoke"];
            Assert.That(chain.Find("ps_texture").Textures["diffuse"], Is.EqualTo(@"vfx\textures\smoke_dense_loop_diffuse.dds"));
            Assert.That(chain.Find("vs_animated_texture").Properties["no_cells_xy"].Float2(Vector2.One), Is.EqualTo(new Vector2(8, 4)));
        }

        [Test]
        public void RenderStack_KeepsItsSceneViewType() =>
            Assert.That(Parse(Sample).SceneViewTypes, Is.EqualTo(new Dictionary<string, string> { ["ripple"] = "DISTORTION" }));

        [Test]
        public void Reference_ReadsTransformRangesAndOverrides()
        {
            var reference = Parse(Sample).References.Single();
            Assert.Multiple(() =>
            {
                Assert.That(reference.Target, Is.EqualTo("wh3_dlc23_lib_warmachines"));
                Assert.That((reference.ScaleMin, reference.ScaleMax, reference.StartTimeMin, reference.SimulationSpeed), Is.EqualTo((1f, 2f, -1f, 0.5f)));
                Assert.That((reference.Position, reference.RotationDegrees), Is.EqualTo((new Vector3(0.5f, 0, 0), new Vector3(0, 90, 0))));
                Assert.That(reference.InfiniteLife, Is.True);
                Assert.That(reference.Overrides["base_red_smoke"].Enable, Is.False);
                var fire = reference.Overrides["furnace_fire"];
                Assert.That((fire.TintMin.X, fire.ParticleLifetimeBiasMax, fire.SpawnRateScaleMin), Is.EqualTo((0.5f, 0.2f, 2f)));
            });
        }

        [Test]
        public void Utf8WithoutBom_ParsesTheSame() =>
            Assert.That(VfxFile.Parse(Encoding.UTF8.GetBytes(Sample)).Emitters.Single().Id, Is.EqualTo("smoke"));
    }
}
