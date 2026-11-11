using Microsoft.Xna.Framework;
using NUnit.Framework;
using Shared.GameFormats.Bmd;
using Shared.GameFormats.RigidModel.Transforms;

namespace FileTypesTests.FileTypes.Bmd
{
    /// <summary>
    /// No vanilla WH3 .bmd has data in the battle-only sections (capture locations, toggleable slots, catchment
    /// areas, ...), so these tests are the only thing exercising their readers and writers. They check that the
    /// parser and writer agree with each other; the layouts themselves come from the assembly kit's serialisers.
    /// </summary>
    public class BmdRoundTripTests
    {
        [TestCase((ushort)12, (ushort)7)]
        [TestCase((ushort)1, (ushort)6)]
        [TestCase((ushort)5, (ushort)3)]
        public void EveryDecodedSectionRoundTrips(ushort captureLocationVersion, ushort slotVersion)
        {
            var file = BuildFile(captureLocationVersion, slotVersion);

            var bytes = BmdWriter.Write(file);
            var reread = BmdParser.Parse(bytes);
            var rewritten = BmdWriter.Write(reread);

            Assert.That(rewritten, Is.EqualTo(bytes));

            var location = reread.CaptureLocationLists.Single().Locations.Single();
            Assert.That(location.DatabaseKey, Is.EqualTo("cap_key"));
            Assert.That(location.Radius, Is.EqualTo(12.5f));
            Assert.That(location.LocationPoints, Has.Count.EqualTo(2));
            Assert.That(location.BuildingIndices, Has.Count.EqualTo(captureLocationVersion == 1 ? 2 : 0));
            Assert.That(location.BuildingLinks, Has.Count.EqualTo(captureLocationVersion > 1 ? 1 : 0));
            Assert.That(location.InitialOwnerType, Is.EqualTo(captureLocationVersion > 11 ? "owner" : ""));

            var slot = reread.ToggleableBuildingSlots.Single();
            Assert.That(slot.CompositeScenes.Single().SceneFile, Is.EqualTo("scene.csc"));
            Assert.That(slot.Outlines.Single(), Has.Count.EqualTo(2));
            Assert.That(slot.MeleeAttackAllowed, Is.EqualTo(slotVersion == 6));

            Assert.That(reread.BattlefieldBuildingFars.Single().BuildingKey, Is.EqualTo("far_building"));
            Assert.That(reread.EFLines.Single().Purpose, Is.EqualTo("purpose"));
            Assert.That(reread.ZonesTemplates.Single().Lines.Single().Orientation, Is.EqualTo(1.25f));
            Assert.That(reread.BmdOutlines.Single().Points, Has.Count.EqualTo(2));
            Assert.That(reread.CameraZones.Single().Points, Has.Count.EqualTo(2));
            Assert.That(reread.CivilianDeployments.Single().CivilianAmount, Is.EqualTo("many"));
            Assert.That(reread.CivilianShelters.Single().CivilianDeploymentLink, Is.EqualTo(3));
            Assert.That(reread.AiHints.Separators.Single().Points, Has.Count.EqualTo(2));
            Assert.That(reread.AiHints.DirectedPoints.Single().Direction.Y, Is.EqualTo(1f));
            Assert.That(reread.AiHints.PolyLinesList.Select(p => p.District), Is.EqualTo(new uint[] { 0, 9 }));
            Assert.That(reread.TerrainStencilBlendTriangles.Single().HeightMode, Is.EqualTo("HM_TERRAIN"));
            Assert.That(reread.BmdCachedAreas.Single().ValidLocationFlags.ValidWest, Is.True);
            Assert.That(reread.BmdCachedAreas.Single().BattleType, Is.EqualTo("land_normal"));
            Assert.That(reread.TerraindDecals.Single().Splines.Single().Cp3.Z, Is.EqualTo(3f));
            Assert.That(reread.TreeListReferences.Single().Climate, Is.EqualTo("temperate"));
            Assert.That(reread.GrassListReferences.Single().File, Is.EqualTo("grass.xml"));
            Assert.That(reread.WaterOutlines.Single().VertexList, Has.Count.EqualTo(2));
            Assert.That(reread.TerrainOutlines.Single().VertexList, Has.Count.EqualTo(2));
            Assert.That(reread.LiteBuildingOutlines.Single().VertexList, Has.Count.EqualTo(2));
        }

        // The kit's prop serialiser puts 30, 31 and 23 bytes after the transform for versions 1, 2 and 3.
        [TestCase((ushort)1, 30)]
        [TestCase((ushort)2, 31)]
        [TestCase((ushort)3, 23)]
        public void OldPropsHaveTheKitsSize(ushort propVersion, int bytesAfterTransform)
        {
            var without = BmdWriter.Write(BuildFile(12, 7));
            var withProp = BmdWriter.Write(WithProp(BuildFile(12, 7), new PropInfo { PropInfoVersion = propVersion, Rmv2Path = "a" }));

            const int propHeader = 2 + 2 + 1 + 48; // version, path string ("a"), 4x3 transform
            Assert.That(withProp.Length - without.Length, Is.EqualTo(propHeader + bytesAfterTransform));
        }

        [Test]
        public void OldPropFieldsRoundTrip()
        {
            var prop = new PropInfo
            {
                PropInfoVersion = 2,
                Rmv2Path = "decal.rigid_model_v2",
                IsDecal = true,
                DecalParallax = 0.5f,
                DecalScale = new RmvVector3(1, 2, 3),
                DecalTiled = true,
                IsFauna = true,
                Animated = true,
                Flags = new BmdComponentFlags { ClampToSurface = true, AllowInOutfield = true, SeasonWinter = true },
            };

            var reread = BmdParser.Parse(BmdWriter.Write(WithProp(BuildFile(12, 7), prop))).PropInfos.Single();

            Assert.That(reread.DecalParallax, Is.EqualTo(0.5f));
            Assert.That(reread.DecalScale.Z, Is.EqualTo(3f));
            Assert.That(reread.DecalTiled, Is.True);
            Assert.That(reread.Animated, Is.True);
            Assert.That(reread.Flags.ClampToSurface, Is.True);
            Assert.That(reread.Flags.AllowInOutfield, Is.True);
            Assert.That(reread.Flags.SeasonWinter, Is.True);
            Assert.That(reread.Flags.SeasonSpring, Is.False);
        }

        private static BmdFile WithProp(BmdFile file, PropInfo prop)
        {
            file.PropInfos.Add(prop);
            return file;
        }

        private static List<RmvVector2> TwoPoints() => [new RmvVector2(1, 2), new RmvVector2(3, 4)];

        private static BmdFile BuildFile(ushort captureLocationVersion, ushort slotVersion)
        {
            var file = new BmdFile { Header = new FastBinHeader { FastBin0 = "FASTBIN0", FastBinVersion = 27 } };
            foreach (var key in new[] { "BattlefieldBuilding", "BattlefieldBuildingFar", "ZonesTemplate", "BmdInfo", "BmdOutline",
                         "CameraZone", "VfxInfo", "AiHints", "AiHints.Separators", "AiHints.DirectedPoints", "AiHints.PolyLines",
                         "AiHints.PolyLinesList", "LightProbe", "TerrainHole", "PointLight", "BuildingProjectileEmitter", "PolyMesh",
                         "TerrainStencilBlendTriangle", "SpotLight", "Sound", "CSC", "Deployment", "BmdCachedArea", "TerraindDecal",
                         "TreeListReference", "GrassListReference" })
                file.SectionVersions[key] = 1;
            file.SectionVersions["Prop"] = 1; // inline paths, so the old props below need no string table
            file.SectionVersions["CaptureLocation"] = captureLocationVersion;
            file.SectionVersions["ToggleableBuildingSlot"] = slotVersion;

            file.BattlefieldBuildingFars.Add(new BattlefieldBuilding
            {
                Version = 11,
                BuildingKey = "far_building",
                Properties = new BuildingProperties { Version = 13 },
            });

            file.CaptureLocationLists.Add(new CaptureLocationList { Locations = [new CaptureLocation
            {
                Location = new RmvVector2(10, 20),
                Radius = 12.5f,
                ValidForMinNumPlayers = 1,
                ValidForMaxNumPlayers = 8,
                CapturePointType = "cpt",
                RestoreType = captureLocationVersion > 2 ? "restore" : "",
                LocationPoints = TwoPoints(),
                BuildingIndices = captureLocationVersion == 1 ? [4, 5] : [],
                DatabaseKey = "cap_key",
                FlagFacing = new RmvVector2(0, 1),
                DestroyBuildingOnCapture = captureLocationVersion >= 4,
                DisableBuildingAbilitiesWhenNoOriginalOwner = captureLocationVersion >= 6,
                AbilitiesAffectGlobally = captureLocationVersion >= 5,
                BuildingLinks = captureLocationVersion > 1 ? [new BuildingLink { Version = 3, BuildingIndex = 2, PrefabIndex = -1, PrefabBuildingKey = "pb", Uid = 7, PrefabUid = 8 }] : [],
                ToggleSlotsLinks = captureLocationVersion >= 7 ? [1] : [],
                AiHintsLinks = captureLocationVersion >= 8 ? [2, 3] : [],
                ScriptId = captureLocationVersion >= 9 ? "script" : "",
                IsTimeBased = captureLocationVersion >= 10,
                InitialOwnerType = captureLocationVersion > 11 ? "owner" : "",
            }] });

            file.EFLines.Add(new EFLine { Label = "line", End1 = new RmvVector3(1, 2, 3), End2 = new RmvVector3(4, 5, 6), Direction = new RmvVector3(0, 0, 1), Purpose = "purpose" });
            file.ZonesTemplates.Add(new ZonesTemplate
            {
                Outline = TwoPoints(),
                ZoneName = "zone",
                EntityFormationTemplateName = "eft",
                Lines = [new ZoneLine { Label = "zl", Start = new RmvVector3(1, 1, 1), End = new RmvVector3(2, 2, 2), Purpose = "p", Orientation = 1.25f }],
                Transform = Matrix.Identity,
            });
            file.BmdOutlines.Add(new BmdOutline { Version = 1, Points = TwoPoints() });
            file.TerrainOutlines.Add(new TerrainOutline { VertexList = TwoPoints() });
            file.LiteBuildingOutlines.Add(new LiteBuildingOutline { VertexList = TwoPoints() });
            file.CameraZones.Add(new CameraZone { Version = 1, Points = TwoPoints() });
            file.CivilianDeployments.Add(new CivilianDeployment { Version = 1, CivilianType = "peasant", CivilianAmount = "many", Outline = TwoPoints() });
            file.CivilianShelters.Add(new CivilianShelter { Version = 1, Position = new RmvVector2(1, 1), Entrance = new RmvVector2(2, 2), CivilianDeploymentLink = 3 });

            file.AiHints.Separators.Add(new Separator { Version = 1, Type = "sep", Points = TwoPoints() });
            file.AiHints.DirectedPoints.Add(new DirectedPoint { Version = 1, Type = "dp", Position = new RmvVector2(5, 6), Direction = new RmvVector2(0, 1) });
            file.AiHints.PolyLinesList.Add(new HintPolyLineList { Version = 1, Type = "old", District = 0, PolygonList = [new Polygon { Points = TwoPoints() }] });
            file.AiHints.PolyLinesList.Add(new HintPolyLineList { Version = 2, Type = "new", District = 9, PolygonList = [new Polygon { Points = TwoPoints() }] });

            file.TerrainStencilBlendTriangles.Add(new TerrainStencilBlendTriangle { Version = 2, Position0 = new RmvVector3(1, 0, 0), Position1 = new RmvVector3(0, 1, 0), Position2 = new RmvVector3(0, 0, 1), HeightMode = "HM_TERRAIN" });

            file.BmdCachedAreas.Add(new BmdCachedArea
            {
                Version = 6,
                Aabb = [-1.5f, -2, 3, 4.5f],
                DefendingFactionRestriction = "dfr",
                Name = "name",
                BattleType = "land_normal",
                ValidLocationFlags = new ValidLocationFlags { Version = 1, ValidNorth = true, ValidWest = true },
            });

            file.ToggleableBuildingSlots.Add(new ToggleableBuildingSlot
            {
                ToggleableSlotType = "slot",
                BuildingLinks = [new BuildingLink { Version = 1, BuildingIndex = 1, PrefabIndex = 2, PrefabBuildingKey = "k" }],
                CompositeScenes = [new CscInfo { Version = 12, SceneFile = "scene.csc" }],
                Outlines = [TwoPoints()],
                ScriptId = slotVersion >= 4 ? "s" : "",
                MapBarrierRecordKey = slotVersion >= 5 ? "barrier" : "",
                MeleeAttackAllowed = slotVersion == 6,
                GroundMeleeAttackAllowed = slotVersion >= 7,
            });

            file.TerraindDecals.Add(new TerraindDecal
            {
                Version = 2,
                SplineVersion = 1,
                VertexSpacing = new RmvVector2(1, 1),
                UvScale = 2,
                Splines = [new TerrainDecalSpline { Version = 1, StartWidth = 1, EndWidth = 2, Cp0 = new RmvVector3(0, 0, 0), Cp1 = new RmvVector3(1, 1, 1), Cp2 = new RmvVector3(2, 2, 2), Cp3 = new RmvVector3(3, 3, 3) }],
                Material = "mat",
                Transform = Matrix.CreateTranslation(1, 2, 3),
            });

            file.TreeListReferences.Add(new TreeListReference { Version = 1, File = "trees.xml", Climate = "temperate" });
            file.GrassListReferences.Add(new GrassListReference { Version = 1, File = "grass.xml", Climate = "temperate" });
            file.WaterOutlines.Add(new WaterOutline { VertexList = TwoPoints() });
            return file;
        }
    }
}
