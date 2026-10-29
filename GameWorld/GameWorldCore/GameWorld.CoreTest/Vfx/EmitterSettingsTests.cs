using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using static GameWorld.Core.Test.Vfx.VfxTestFiles;

namespace GameWorld.Core.Test.Vfx
{
    public class EmitterSettingsTests
    {
        // Cut from wh3_dlc23_lib_warmachines: a premultiplied smoke flipbook and an additive atlas spark.
        const string SmokeChain = """<behaviour_chain id="smoke"><quality value="0"><modifier fragment="ps_texture"><texture_property name="diffuse" type="file" path="vfx\textures\smoke_dense_loop_diffuse.dds"/><property name="pre_multiplied_alpha" type="bool" value="true"/></modifier><modifier fragment="vs_animated_texture"><property name="no_cells_xy" type="float2" value="float2(8.0,8.0)"/><property name="loop" type="bool" value="true"/><property name="no_loops" type="float2" value="float2(0.5,0.5)"/><property name="time_mode" type="list" value="using_particle_lifetime"/></modifier><modifier fragment="ps_lighting"><property name="additive_blend_mul" type="float" value="0.0"/></modifier><modifier fragment="vs_translation"><property name="pivot_x" type="float" value="0.5"/><property name="pivot_y" type="float" value="0.25"/></modifier></quality></behaviour_chain>""";
        const string SparksChain = """<behaviour_chain id="sparks"><quality value="0"><modifier fragment="ps_texture"><texture_property name="diffuse" type="file" path="vfx\textures\sparks.dds"/><property name="pre_multiplied_alpha" type="bool" value="false"/></modifier><modifier fragment="vs_translation_velocity_v2"><property name="velocity_stretch_mul" type="curve">x_min,0,0;0.0,0.5,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,1;0,1</property><property name="orientation_mode" type="list" value="Camera-aligned with stretching factor"/><property name="velocity_stretch_cap" type="float" value="3.0"/></modifier><modifier fragment="vs_cell_texture"><property name="atlas_number_of_cells_x" type="int" value="8"/><property name="atlas_number_of_cells_y" type="int" value="8"/><property name="atlas_cell" type="int" value="51"/><property name="number_of_cells_x" type="int" value="2"/><property name="number_of_cells_y" type="int" value="2"/><property name="randomize_cell" type="bool" value="true"/></modifier><modifier fragment="ps_lighting"><property name="additive_blend_mul" type="float" value="1.0"/></modifier></quality></behaviour_chain>""";

        static List<EmitterSettings> Read(string emitters, string chains, string stacks = "") =>
            new VfxResolver(Pack(("fx.xml", Xml(emitters: emitters, chains: chains, stacks: stacks)))).Resolve("fx", new Random(1)).Emitters.Select(EmitterSettings.From).ToList();

        [Test]
        public void SmokeChain_IsAPremultipliedFlipbook_InLocalSpace()
        {
            var smoke = Read(Emitter("smoke", """<property name="ParticleSimulationSpace" quality="0" value="LOCAL_SPACE_POSITION_ROTATION"/>"""), SmokeChain).Single();
            Assert.Multiple(() =>
            {
                Assert.That(smoke.Draws, Is.True);
                Assert.That(smoke.Space, Is.EqualTo(SimulationSpace.LocalPositionRotation));
                Assert.That(smoke.InfiniteLife, Is.True);
                Assert.That(smoke.Texture, Is.EqualTo(@"vfx\textures\smoke_dense_loop_diffuse.dds"));
                Assert.That((smoke.Premultiplied, smoke.AdditiveMul), Is.EqualTo((true, 0f)));
                Assert.That((smoke.Animated, smoke.CellsX, smoke.CellsY, smoke.FlipbookLoop, smoke.FixedSpeed), Is.EqualTo((true, 8, 8, true, false)));
                Assert.That(smoke.Loops, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(smoke.Pivot, Is.EqualTo(new Vector2(0.5f, 0.25f)));
                Assert.That(smoke.Orientation, Is.EqualTo(QuadOrientation.CameraFacing));
            });
        }

        // A reference's infinite_life="false" ends what it reaches, however deep: wh3_main_lib_lightsource's eight lights are
        // all infinite, and explosions reference it with false. "true" makes nothing infinite: 659 impact and explosion
        // references say it over emitters that all end.
        [Test]
        public void ReferenceInfiniteLife_EndsWhatItReaches_AndMakesNothingInfinite()
        {
            var lib = Xml(emitters: Emitter("lit") + """<emitter type="continuous_mesh" id="flash"></emitter>""");
            var mid = Xml(references: Reference("lib", infinite: true));
            var fx = Xml(references: Reference("lib", infinite: false) + Reference("lib", infinite: true) + Reference("mid", infinite: false));
            var settings = new VfxResolver(Pack(("lib.xml", lib), ("mid.xml", mid), ("fx.xml", fx))).Resolve("fx", new Random(1)).Emitters.Select(EmitterSettings.From);
            Assert.That(settings.Select(x => x.InfiniteLife), Is.EqualTo(new[] { false, false, true, false, false, false }));
        }

        // ps_lighting's five settings, as smoke_heavy (wh_main_lib_fire_smoke) sets them. A chain blended by alpha draws lit; an
        // additive one and a decal do not, since the preview's additive reading is not CA's.
        [Test]
        public void Lighting_IsReadOffPsLighting_AndOnlyAnAlphaBlendedChainIsLit()
        {
            var chain = SmokeChain.Replace("""value="0.0"/></modifier><modifier fragment="vs_translation">""",
                """value="0.0"/><property name="small_particles" type="float" value="0.8"/><property name="large_particles" type="float" value="0.0"/><property name="directionality" type="float" value="0.4"/><property name="emissive" type="float" value="0.0"/><property name="average_size" type="float" value="0.8"/></modifier><modifier fragment="vs_translation">""");
            var smoke = Read(Emitter("smoke"), chain).Single();
            var sparks = Read(Emitter("sparks"), SparksChain).Single();
            Assert.Multiple(() =>
            {
                Assert.That(smoke.Lighting, Is.EqualTo(new ParticleLighting(0.8f, 0, 0.4f, 0, 0.8f)));
                Assert.That(smoke.Lit, Is.True);
                Assert.That((sparks.Lighting != null, sparks.Lit), Is.EqualTo((true, false)));
                Assert.That(new EmitterSettings { Lighting = smoke.Lighting, Decal = true }.Lit, Is.False);
            });
        }

        // ps_normal: the chain's ps_texture normal map shapes its lighting. Without ps_normal the map is not read, as in CA's ps_main.
        [Test]
        public void NormalMap_IsReadWhereTheChainHasPsNormal()
        {
            var mapped = SmokeChain.Replace("""<property name="pre_multiplied_alpha" """, """<texture_property name="normal" type="file" path="vfx\textures\smoke_n.dds"/><property name="pre_multiplied_alpha" """);
            var withNormal = Read(Emitter("smoke"), mapped.Replace("</quality>", """<modifier fragment="ps_normal"/></quality>""")).Single();
            var without = Read(Emitter("smoke"), mapped).Single();
            Assert.That((withNormal.NormalMap, without.NormalMap), Is.EqualTo((@"vfx\textures\smoke_n.dds", (string?)null)));
        }

        [Test]
        public void SparksChain_IsAStretchedAdditiveAtlasCell()
        {
            var sparks = Read(Emitter("sparks"), SparksChain).Single();
            Assert.Multiple(() =>
            {
                Assert.That(sparks.Space, Is.EqualTo(SimulationSpace.World), "no ParticleSimulationSpace is world space");
                Assert.That((sparks.Orientation, sparks.StretchCap), Is.EqualTo((QuadOrientation.VelocityAligned, 3f)));
                Assert.That(sparks.Stretch, Is.Not.Null);
                Assert.That((sparks.AtlasX, sparks.AtlasY, sparks.AtlasCell, sparks.CellsX, sparks.CellsY, sparks.RandomCell, sparks.Animated), Is.EqualTo((8, 8, 51, 2, 2, true, false)));
                Assert.That((sparks.Premultiplied, sparks.AdditiveMul), Is.EqualTo((false, 1f)));
            });
        }

        // A DEFERRED_DECAL paints the ground's colour, so it is laid over by its alpha whatever pre_multiplied_alpha says:
        // wh3_dlc20_kho_crack_01, flagged premultiplied, bleeds brown into its clear texels and drew a brown square. An
        // emissive decal is light, and keeps the flag.
        [TestCase("DEFERRED_DECAL", false)]
        [TestCase("DEFERRED_DECAL_EMISSIVE", true)]
        public void DecalStack_PaintsTheGround_ByItsAlpha(string view, bool premultiplied)
        {
            var decal = Read(Emitter("decal"), SmokeChain.Replace("\"smoke\"", "\"decal\""),
                $"""<effect_render_stack id="decal"><property name="scene_view_type" quality="0" value="{view}"/></effect_render_stack>""").Single();
            Assert.That((decal.Decal, decal.Premultiplied), Is.EqualTo((true, premultiplied)));
        }

        [Test]
        public void DistortionStack_IsNotDrawn_AndSaysWhy()
        {
            var ripple = Read(Emitter("ripple"), SmokeChain.Replace("\"smoke\"", "\"ripple\""),
                """<effect_render_stack id="ripple"><property name="scene_view_type" quality="0" value="DISTORTION"/></effect_render_stack>""").Single();
            Assert.That(ripple.Draws, Is.False);
            Assert.That(ripple.Skipped, Has.One.Matches<string>(x => x.Contains("DISTORTION")));
        }

        // A continuous_rigid emitter draws its RigidData models, with or without a chain; an AnimFile plays, so it reports
        // nothing. Its orientation and spin are read on all three axes.
        [Test]
        public void RigidEmitter_DrawsItsModels_TurningOnEveryAxis()
        {
            var properties = """<property_rigid_data name="RigidData" quality="0"><rigid_entry><ModelFile value="vfx/models/rat.wsmodel"/><PivotPoint x="0.50" y="0.50" z="0.50"/><AnimFile value="animations/rat.anim"/><AnimSpeed value="1.00"/></rigid_entry></property_rigid_data><property name="ParticleOrientationMin" quality="0" x="10.00" y="20.00" z="30.00"/><property name="ParticleOrientationMax" quality="0" x="40.00" y="50.00" z="60.00"/><property name="ParticleSpin_Min" quality="0" x="1.00" y="2.00" z="3.00"/><property name="ParticleSpin_Max" quality="0" x="4.00" y="5.00" z="6.00"/><property name="ParticleSpinDirectionX" quality="0" value="CW"/>""";
            var rat = Read(Emitter("rat", properties).Replace("continuous_mesh", "continuous_rigid"), "").Single();
            Assert.Multiple(() =>
            {
                Assert.That(rat.Draws, Is.True);
                Assert.That(rat.Rigid.Select(x => x.ModelFile), Is.EqualTo(new[] { "vfx/models/rat.wsmodel" }));
                Assert.That((rat.TurnXy, rat.SpinXy, rat.SpinSignXy), Is.EqualTo(((new Vector2(10, 20), new Vector2(40, 50)), (new Vector2(1, 2), new Vector2(4, 5)), new Vector2(-1, 1))));
                Assert.That(rat.Skipped, Has.None.Matches<string>(x => x.Contains("animations/rat.anim")), "the animation plays");
            });
        }

        // SpawnInheritedDirectionBlend is read, so it reports nothing. Wind is not simulated, so WindVarianceFactor says so on its
        // own (453 vanilla emitters set it with no EffectedByWindFactor). ref_spawn_speed, on the <vfx> element of 11 flamethrower
        // and acid effects, is not read, and says so; -1, on the other 7,459, is unset.
        [Test]
        public void DirectionBlendIsRead_AndWindVarianceAndRefSpawnSpeedSaySo()
        {
            var properties = """<property name="SpawnInheritedDirectionBlend" quality="0" value="0.50"/><property name="WindVarianceFactor" quality="0" value="0.30"/>""";
            var chain = SmokeChain.Replace("\"smoke\"", "\"e\"");
            var flame = ReadFile(Xml(emitters: Emitter("e", properties), chains: chain, root: " ref_spawn_speed=\"30.00\""));
            var plain = ReadFile(Xml(emitters: Emitter("e"), chains: chain, root: " ref_spawn_speed=\"-1.00\""));
            Assert.Multiple(() =>
            {
                Assert.That(flame.InheritedDirectionBlend, Is.EqualTo(0.5f));
                Assert.That(flame.Skipped, Has.None.Contains("SpawnInheritedDirectionBlend"));
                Assert.That(flame.Skipped, Has.One.Contains("wind"));
                Assert.That(flame.Skipped, Has.One.Contains("ref_spawn_speed 30"));
                Assert.That(plain.Skipped, Has.None.Contains("ref_spawn_speed"));
            });
        }

        static EmitterSettings ReadFile(byte[] file) =>
            EmitterSettings.From(new VfxResolver(Pack(("fx.xml", file))).Resolve("fx", new Random(1)).Emitters.Single());

        // A DISTORTION chain names only a normal map; that is what it draws with.
        [Test]
        public void DistortionStack_DrawsWithItsNormalMap()
        {
            var ripple = Read(Emitter("ripple"), SmokeChain.Replace("\"smoke\"", "\"ripple\"").Replace("""path="vfx\textures\smoke_dense_loop_diffuse.dds"/>""", """path=""/><texture_property name="normal" type="file" path="vfx\textures\shockwave_normal.dds"/>"""),
                """<effect_render_stack id="ripple"><property name="scene_view_type" quality="0" value="DISTORTION"/></effect_render_stack>""").Single();
            Assert.That((ripple.Draws, ripple.Distortion, ripple.Texture), Is.EqualTo((true, true, @"vfx\textures\shockwave_normal.dds")));
        }

        // VectorFieldStrId names a volume in the packs: loaded, the field is simulated; missing, the preview says which.
        [Test]
        public void VectorField_IsLoaded_OrSaysWhyNot()
        {
            var properties = """<property name="VectorFieldStrId" quality="0" value="vfx/vectorfields/swirl.dds"/><property name="VectorFieldSize" quality="0">x_min,0,0;0.0,4.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,4.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1</property>""";
            var pack = Pack(("fx.xml", Xml(emitters: Emitter("smoke", properties), chains: SmokeChain)), ("vectorfields/swirl.dds", Field(2, 2, 2, (_, _, _) => Vector3.UnitY)));
            var loaded = new VfxResolver(pack).Resolve("fx", new Random(1)).Emitters.Select(EmitterSettings.From).Single();
            var missing = Read(Emitter("smoke", properties), SmokeChain).Single();
            Assert.Multiple(() =>
            {
                Assert.That((loaded.Field?.Sample(Vector3.Zero), loaded.FieldSize?.Evaluate(0, 0, 0)), Is.EqualTo(((Vector3?)Vector3.UnitY, (float?)4)));
                Assert.That(loaded.Skipped, Has.None.Matches<string>(x => x.Contains("vector field")));
                Assert.That((missing.Draws, missing.Field), Is.EqualTo((true, (VfxVectorField?)null)));
                Assert.That(missing.Skipped, Has.One.Matches<string>(x => x.Contains("vfx/vectorfields/swirl.dds")));
            });
        }

        // DEFERRED_DYNAMIC_LIGHT: the particle is a light (249 of the 357 chains carry no ps_texture), drawn as the pool it
        // casts on the ground. It lights the models too (RenderEngineComponent hands it to their shaders), so nothing is reported.
        [Test]
        public void DynamicLightStack_DrawsAsAPoolOnTheGround()
        {
            var light = Read(Emitter("glow"), """<behaviour_chain id="glow"><quality value="0"><modifier fragment="vs_translation"><property name="pivot_x" type="float" value="0.5"/><property name="pivot_y" type="float" value="0.5"/></modifier></quality></behaviour_chain>""",
                """<effect_render_stack id="glow"><property name="scene_view_type" quality="0" value="DEFERRED_DYNAMIC_LIGHT"/></effect_render_stack>""").Single();
            Assert.That((light.Draws, light.Light, light.Decal, light.Texture), Is.EqualTo((true, true, true, (string?)null)));
            Assert.That(light.Skipped, Has.None.Matches<string>(x => x.Contains("lights only the ground")));
        }

        [TestCase("DEFERRED_DECAL")]
        [TestCase("DEFERRED_DECAL_EMISSIVE")]
        [TestCase("SEA_OVERLAY")]
        public void GroundStacks_AreDrawnAsDecals(string view)
        {
            var scorch = Read(Emitter("smoke"), SmokeChain,
                $"""<effect_render_stack id="smoke"><property name="scene_view_type" quality="0" value="{view}"/></effect_render_stack>""").Single();
            Assert.That((scorch.Draws, scorch.Decal), Is.EqualTo((true, true)));
            Assert.That(Read(Emitter("smoke"), SmokeChain).Single().Decal, Is.False);
        }

        [TestCase("Vertically-oriented", QuadOrientation.Vertical)]
        [TestCase("Camera-facing velocity-aligned", QuadOrientation.VelocityAxis)]
        [TestCase("Particle only rotating around local Y (horizontal)", QuadOrientation.LocalHorizontal)]
        [TestCase("Particle only rotating around local Y (vertical)", QuadOrientation.LocalVertical)]
        public void RareOrientationModes_AreRead(string mode, QuadOrientation expected)
        {
            var sparks = Read(Emitter("sparks"), SparksChain.Replace("Camera-aligned with stretching factor", mode)).Single();
            Assert.That((sparks.Orientation, sparks.Skipped.Any(x => x.Contains("orientation_mode"))), Is.EqualTo((expected, false)));
        }

        // vs_translation's restriction, from CA's fx/particle_vfx_library.hlsl: "CameraFacing=0,Vertical,Horizontal,3D". 948
        // vanilla chains set it off 0, and every one of them drew camera-facing.
        [TestCase("0", QuadOrientation.CameraFacing)]
        [TestCase("1", QuadOrientation.Upright)]
        [TestCase("2", QuadOrientation.Flat)]
        [TestCase("3", QuadOrientation.Object3D)]
        public void Restriction_IsRead(string restriction, QuadOrientation expected)
        {
            const string pivot = """<property name="pivot_y" type="float" value="0.25"/>""";
            var smoke = Read(Emitter("smoke"), SmokeChain.Replace(pivot, pivot + $"""<property name="restriction" type="int" value="{restriction}"/>""")).Single();
            Assert.That(smoke.Orientation, Is.EqualTo(expected));
        }

        [Test]
        public void OverlayColour_IsRead()
        {
            var overlaid = SmokeChain.Replace("""value="true"/></modifier>""", """value="true"/><property name="overlay_colour" type="bool" value="true"/></modifier>""");
            Assert.That((Read(Emitter("smoke"), overlaid).Single().Overlay, Read(Emitter("smoke"), SmokeChain).Single().Overlay), Is.EqualTo((true, false)));
        }

        [Test]
        public void FlipbookRows_AreRead()
        {
            var rows = SmokeChain.Replace("""<property name="loop" type="bool" value="true"/>""", """<property name="loop" type="bool" value="true"/>"""
                + """<property name="row_restricted" type="bool" value="true"/><property name="row_restriction" type="int" value="2"/><property name="randomize_row" type="bool" value="true"/>""");
            var (read, plain) = (Read(Emitter("smoke"), rows).Single(), Read(Emitter("smoke"), SmokeChain).Single());
            Assert.That((read.RowRestricted, read.Row, read.RandomRow, plain.RowRestricted, plain.Row, plain.RandomRow), Is.EqualTo((true, 2, true, false, 0, false)));
        }

        [Test]
        public void FlipbookRandomCell_IsRead()
        {
            var random = SmokeChain.Replace("""<property name="loop" type="bool" value="true"/>""", """<property name="loop" type="bool" value="true"/><property name="randomize_cell" type="bool" value="true"/>""");
            Assert.That((Read(Emitter("smoke"), random).Single().RandomCell, Read(Emitter("smoke"), SmokeChain).Single().RandomCell), Is.EqualTo((true, false)));
        }

        [Test]
        public void AlphaOffsets_AreRead()
        {
            var offsets = SmokeChain.Replace("""<modifier fragment="ps_lighting">""", """<modifier fragment="vs_vertex_alpha">"""
                + """<property name="alpha_START_offset" type="float2" value="float2(0.1,0.3)"/><property name="alpha_END_offset" type="float2" value="float2(0.7,0.8)"/>"""
                + """</modifier><modifier fragment="ps_lighting">""");
            var (read, plain) = (Read(Emitter("smoke"), offsets).Single(), Read(Emitter("smoke"), SmokeChain).Single());
            Assert.That((read.AlphaStartOffset, read.AlphaEndOffset, plain.AlphaStartOffset, plain.AlphaEndOffset),
                Is.EqualTo((new Vector2(0.1f, 0.3f), new Vector2(0.7f, 0.8f), Vector2.Zero, Vector2.Zero)));
        }

        [TestCase("RELATIVE_TO_TERRAIN_NORMAL", true)]
        [TestCase("DISABLED", false)]
        public void RelativeSpawnDirection_IsRead(string value, bool onTerrain)
        {
            var emitter = Emitter("smoke", $"""<property name="ParticleRelativeSpawnDirection" quality="0" value="{value}"/>""");
            Assert.That((Read(emitter, SmokeChain).Single().DirectionOnTerrain, Read(Emitter("smoke"), SmokeChain).Single().DirectionOnTerrain), Is.EqualTo((onTerrain, false)));
        }

        [Test]
        public void VectorFieldLoopFlags_AreRead()
        {
            var looped = Read(Emitter("smoke", """<property name="VectorFieldStrId" quality="0" value="vfx/vectorfields/00_turbulence_8_xyz.dds"/><property name="VectorfieldSizeLoopAnim" quality="0" value="true"/><property name="VectorfieldPositionLoopAnim" quality="0" value="true"/><property name="VectorfieldOrientationLoopAnim" quality="0" value="true"/>"""), SmokeChain).Single();
            var plain = Read(Emitter("smoke"), SmokeChain).Single();
            Assert.That((looped.LoopFieldSize, looped.LoopFieldPosition, looped.LoopFieldOrientation, plain.LoopFieldSize, plain.LoopFieldPosition, plain.LoopFieldOrientation),
                Is.EqualTo((true, true, true, false, false, false)));
        }

        [Test]
        public void MaximumVelocity_IsRead() =>
            Assert.That((Read(Emitter("smoke", """<property name="MaximumVelocity" quality="0" value="1.50"/>"""), SmokeChain).Single().MaxSpeed,
                Read(Emitter("smoke"), SmokeChain).Single().MaxSpeed), Is.EqualTo((1.5f, 0f)));

        // ParticleRelativeSpawnOrientation is read on a quad emitter too: a 3D quad stands on the ground as a rigid model does.
        [Test]
        public void RelativeSpawnOrientation_IsReadOnAQuadEmitter()
        {
            var onGround = Read(Emitter("smoke", """<property name="ParticleRelativeSpawnOrientation" quality="0" value="RELATIVE_TO_TERRAIN_NORMAL"/>"""), SmokeChain).Single();
            Assert.That((onGround.RigidOnTerrain, Read(Emitter("smoke"), SmokeChain).Single().RigidOnTerrain), Is.EqualTo((true, false)));
        }

        [Test]
        public void UnknownOrientationMode_DrawsCameraFacing_AndSaysSo()
        {
            var sparks = Read(Emitter("sparks"), SparksChain.Replace("Camera-aligned with stretching factor", "Made up")).Single();
            Assert.That((sparks.Draws, sparks.Orientation), Is.EqualTo((true, QuadOrientation.CameraFacing)));
            Assert.That(sparks.Skipped, Has.One.Matches<string>(x => x.Contains("Made up")));
        }

        [Test]
        public void FlipUv_IsRead()
        {
            var flip = """<modifier fragment="vs_flip_uv"><property name="flip_u" type="float" value="0.5"/><property name="flip_v" type="float" value="1.0"/><property name="uv_orientation" type="list" value="90_degrees_cw"/></modifier>""";
            var sparks = Read(Emitter("sparks"), SparksChain.Replace("</quality>", flip + "</quality>")).Single();
            Assert.That((sparks.FlipU, sparks.FlipV, sparks.UvTurn), Is.EqualTo((0.5f, 1f, -1)));
        }

        [Test]
        public void RenderSizeLimiter_IsRead()
        {
            var limiter = """<modifier fragment="vs_rendersize_limiter"><property name="pixelsize_limit" type="float" value="3.0"/></modifier>""";
            Assert.That(Read(Emitter("sparks"), SparksChain.Replace("</quality>", limiter + "</quality>")).Single().MinPixels, Is.EqualTo(3f));
        }

        [Test]
        public void AlphaCrush_AndGradient_AreRead()
        {
            var crush = """<modifier fragment="ps_alpha_crush"><property name="sharp_transition" type="bool" value="true"/><property name="curve_alpha_crush" type="curve">progress_min,8388608,0;0.0,0.25,linear,linear,-0.05,0.0,0.05,0.0,0:progress,0;0,0</property></modifier>""";
            var gradient = """<modifier fragment="ps_gradient"><property name="curve_colour" type="curve">r_min,16711680,0;0.0,0.5,linear,linear,-0.05,0.0,0.05,0.0,0:r,0;0,0|g,0;0,0|b,0;0,0</property><property name="curve_alpha" type="curve">alpha_min,8388608,0;0.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,0:alpha,0;0,0</property><property name="alpha_mapping" type="bool" value="true"/></modifier>""";
            var e = Read(Emitter("sparks"), SparksChain.Replace("</quality>", crush + gradient + "</quality>")).Single();
            Assert.Multiple(() =>
            {
                Assert.That((e.CrushSharp, e.AlphaCrush?.Evaluate(0, 0.5f, 0)), Is.EqualTo((true, (float?)0.25f)));
                Assert.That((e.Gradient?.Evaluate(1, 0.5f, 0), e.GradientAlpha?.Evaluate(0, 0.5f, 0), e.GradientMapsAlpha), Is.EqualTo(((float?)0.5f, (float?)1f, true)));
            });
            var plain = Read(Emitter("sparks"), SparksChain).Single();
            Assert.That(new object?[] { plain.AlphaCrush, plain.Gradient }, Is.All.Null);
        }

        [Test]
        public void CellBlend_IsRead()
        {
            var blended = SmokeChain.Replace("<property name=\"loop\" type=\"bool\" value=\"true\"/>", "<property name=\"loop\" type=\"bool\" value=\"true\"/><property name=\"cell_blend\" type=\"bool\" value=\"true\"/>");
            Assert.That(Read(Emitter("smoke"), blended).Single().CellBlend, Is.True);
            Assert.That(Read(Emitter("smoke"), SmokeChain).Single().CellBlend, Is.False);
        }

        [Test]
        public void SoftEdge_IsRead()
        {
            var soft = """<modifier fragment="ps_soft_edge"><property name="soft_factor" type="float" value="1.5"/><property name="scale_strength" type="float" value="2.0"/></modifier>""";
            var e = Read(Emitter("sparks"), SparksChain.Replace("</quality>", soft + "</quality>")).Single();
            Assert.That((e.SoftEdge, e.SoftStrength), Is.EqualTo((1.5f, 2f)));
            Assert.That(Read(Emitter("sparks"), SparksChain).Single().SoftEdge, Is.Zero, "no ps_soft_edge: a hard edge");
        }

        [Test]
        public void Emissive_IsRead()
        {
            var emissive = """<modifier fragment="ps_emissive"><property name="emissive_mul" type="float" value="21.0"/></modifier>""";
            Assert.That(Read(Emitter("sparks"), SparksChain.Replace("</quality>", emissive + "</quality>")).Single().EmissiveMul, Is.EqualTo(21f));
            Assert.That(Read(Emitter("sparks"), SparksChain).Single().EmissiveMul, Is.EqualTo(1f), "no ps_emissive: the colour as it is");
        }

        [Test]
        public void CullDistances_AreRead()
        {
            var e = Read(Emitter("sparks", """<property name="CullDistances" quality="0" x="0.00" y="0.00" z="700.00" w="800.00"/><property name="ParticleCullDistances" quality="0" x="2.00" y="5.00" z="400.00" w="450.00"/>"""), SparksChain).Single();
            Assert.That((e.EmitterCull, e.ParticleCull), Is.EqualTo((new Vector4(0, 0, 700, 800), new Vector4(2, 5, 400, 450))));
        }

        [Test]
        public void SkipParticlePositionUpdate_IsRead()
        {
            var head = Read(Emitter("sparks", """<property name="SkipParticlePositionUpdate" quality="0" value="true"/>"""), SparksChain).Single();
            Assert.That((head.SkipPositionUpdate, Read(Emitter("sparks"), SparksChain).Single().SkipPositionUpdate), Is.EqualTo((true, false)));
        }

        [Test]
        public void SpawnSpacing_IsRead()
        {
            var trail = Read(Emitter("sparks", """<property name="SpawnSpacing" quality="0" value="0.50"/>"""), SparksChain).Single();
            Assert.That((trail.SpawnSpacing, Read(Emitter("sparks"), SparksChain).Single().SpawnSpacing), Is.EqualTo((0.5f, 0f)));
        }

        [Test]
        public void TrailMode_IsReadFromTheRenderStack()
        {
            var trail = Read(Emitter("sparks"), SparksChain, """<effect_render_stack id="sparks"><property name="enable_trail_mode" quality="0" value="true"/></effect_render_stack>""").Single();
            Assert.That((trail.Trail, Read(Emitter("sparks"), SparksChain).Single().Trail), Is.EqualTo((true, false)));
        }

        [Test]
        public void EmitterWithoutAChain_IsNotDrawn() => Assert.That(Read(Emitter("lonely"), "").Single().Draws, Is.False);

        [Test]
        public void UnknownFragment_IsNamed_AndTheRestStillDraws()
        {
            var chain = SmokeChain.Replace("<quality value=\"0\">", "<quality value=\"0\"><modifier fragment=\"ps_made_up\"/>");
            var smoke = Read(Emitter("smoke"), chain).Single();
            Assert.That(smoke.Draws, Is.True);
            Assert.That(smoke.Skipped, Has.One.Matches<string>(x => x.Contains("ps_made_up")));
        }

        // 8,908 of 11,722 vanilla continuous_mesh emitters omit SpawnAngle; where present it is an azimuth range
        // (0..360 in 802, -180..180 in 366). Read as 0,0, every emitter without one sprayed into +X alone.
        [Test]
        public void AbsentSpawnAngle_IsEveryAzimuth_AndAPresentOneIsRead()
        {
            Assert.That(Read(Emitter("smoke"), SmokeChain).Single().SpawnAngle, Is.EqualTo(new Vector2(0, 360)));
            var narrow = Read(Emitter("smoke", """<property name="SpawnAngle" quality="0" x="-90.00" y="90.00"/>"""), SmokeChain).Single();
            Assert.That(narrow.SpawnAngle, Is.EqualTo(new Vector2(-90, 90)));
        }

        // SmokeChain with its ps_texture modifier swapped for another.
        static string SmokeWithout(string replacement)
        {
            var start = SmokeChain.IndexOf("<modifier fragment=\"ps_texture\">");
            var end = SmokeChain.IndexOf("</modifier>", start) + "</modifier>".Length;
            return SmokeChain[..start] + replacement + SmokeChain[end..];
        }

        // 231 vanilla chains (wh3_blood_effects among them) draw through a material and carry no ps_texture.
        // Drawn anyway, each particle was a flat quad in the colour curve's tint.
        [Test]
        public void CustomShaderChain_IsNotDrawn_AndNamesItsMaterial()
        {
            var blood = Read(Emitter("smoke"), SmokeWithout("""<modifier fragment="ps_custom_shader"><property name="material" type="file" path="vfx\materials\wh3_blood_spurt_01.xml.material"/></modifier>""")).Single();
            Assert.That(blood.Draws, Is.False);
            Assert.That(blood.Skipped, Has.One.Matches<string>(x => x.Contains("ps_custom_shader") && x.Contains("wh3_blood_spurt_01")));
        }

        // A custom-shader material's colour texture (a slot named for a diffuse or base colour) stands in for its shader. One
        // with none, as the fire's alpha masks, is not drawn and says so.
        [TestCase("t_xml_particle_diffuse_2048_only", true)]
        [TestCase("t_xml_alpha_1", false)]
        public void CustomShaderChain_DrawsItsMaterialsColourTexture(string slot, bool drawn)
        {
            var material = $"<material><textures><texture><slot version=\"2\">t_xml_dither</slot><source>vfx/textures/dither.dds</source></texture><texture><slot version=\"2\">{slot}</slot><source>vfx/Textures/blood_base.dds</source></texture></textures></material>";
            var chain = SmokeWithout("""<modifier fragment="ps_custom_shader"><property name="material" type="file" path="vfx\materials\wh3_blood_spurt_01.xml.material"/></modifier>""");
            var pack = Pack(("fx.xml", Xml(emitters: Emitter("smoke"), chains: chain)), ("materials/wh3_blood_spurt_01.xml.material", System.Text.Encoding.UTF8.GetBytes(material)));
            var blood = new VfxResolver(pack).Resolve("fx", new Random(1)).Emitters.Select(EmitterSettings.From).Single();
            Assert.That((blood.Draws, blood.Texture), Is.EqualTo((drawn, drawn ? "vfx/Textures/blood_base.dds" : null)));
            Assert.That(blood.Skipped, Has.One.Matches<string>(x => x.Contains("ps_custom_shader") && x.Contains("wh3_blood_spurt_01")));
        }

        // The five vanilla drawn chains with no ps_texture all belong to emitters named "sound": they carry a sound and
        // show nothing, where a white quad each was drawn.
        [Test]
        public void ChainWithoutATexture_IsNotDrawn_AndSaysSo()
        {
            var plain = Read(Emitter("smoke"), SmokeWithout("")).Single();
            Assert.That(plain.Draws, Is.False);
            Assert.That(plain.Skipped, Has.One.Matches<string>(x => x.Contains("ps_texture")));
        }

        // A wind simulation stack has no ps_texture either, and is not drawn at all: it says why once, not also that its chain
        // lacks a texture.
        [Test]
        public void UndrawnChainWithoutATexture_SaysOnlyWhyItIsNotDrawn()
        {
            var wind = Read(Emitter("smoke"), SmokeWithout(""),
                """<effect_render_stack id="smoke"><property name="scene_view_type" quality="0" value="WIND_SIMULATION"/></effect_render_stack>""").Single();
            Assert.That(wind.Draws, Is.False);
            Assert.That(wind.Skipped, Has.None.Matches<string>(x => x.Contains("ps_texture")));
        }
    }
}
