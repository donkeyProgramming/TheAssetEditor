using System.Text;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Bmd
{
    /// <summary>
    /// Serializes a <see cref="BmdFile"/> back to FASTBIN0 bytes, mirroring <see cref="BmdParser"/> section
    /// by section. It writes the whole file, not only what the editor exposes, so saving an unedited file
    /// reproduces it exactly. Record layouts follow the WH3 assembly kit's serialisers; the one exception is
    /// FASTBIN &lt; 11, whose early prop/vfx block BmdParser only partly decodes and which can't be written.
    /// The output is re-parsed as a self-check before it is returned.
    /// </summary>
    public static class BmdWriter
    {
        public static byte[] Write(BmdFile bmdFile)
        {
            var v = bmdFile.Header.FastBinVersion;
            if (v < 11)
                throw new NotSupportedException(
                    $"Cannot write FastBin version {v} - files this old mix an early prop/vfx layout into the " +
                    "same lists as the modern one in a way BmdParser can't tell apart once loaded, so a faithful " +
                    "rewrite isn't possible. No real Total War Warhammer prefab this old has been seen in practice " +
                    "(all known campaign prefabs use version 22+).");

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Encoding.UTF8.GetBytes("FASTBIN0"));
                writer.Write(v);

                if (v < 22)
                {
                    var managerVersion = GetVersion(bmdFile, "LegacyPreV22");
                    writer.Write(managerVersion);
                    WriteCollection(writer, bmdFile.LegacyDeploymentAreas, (w, area) => WriteLegacyDeploymentArea(w, area, managerVersion));
                }

                writer.Write(GetVersion(bmdFile, "BattlefieldBuilding"));
                WriteCollection(writer, bmdFile.BattlefieldBuildings, WriteBattlefieldBuilding);

                writer.Write(GetVersion(bmdFile, "BattlefieldBuildingFar"));
                WriteCollection(writer, bmdFile.BattlefieldBuildingFars, WriteBattlefieldBuilding);

                var captureLocationVersion = GetVersion(bmdFile, "CaptureLocation");
                writer.Write(captureLocationVersion);
                WriteCollection(writer, bmdFile.CaptureLocationLists, (w, list) =>
                    WriteCollection(w, list.Locations, (w2, l) => WriteCaptureLocation(w2, l, captureLocationVersion)));

                WriteCollection(writer, bmdFile.EFLines, WriteEFLine);
                WriteCollection(writer, bmdFile.GoOutlines, (w, o) => WritePoints(w, o.VertexList));
                WriteCollection(writer, bmdFile.NonTerrainOutlines, (w, o) => WritePoints(w, o.VertexList));

                writer.Write(GetVersion(bmdFile, "ZonesTemplate"));
                WriteCollection(writer, bmdFile.ZonesTemplates, WriteZonesTemplate);

                writer.Write(GetVersion(bmdFile, "BmdInfo"));
                WriteCollection(writer, bmdFile.BmdInfos, WriteBmdInfo);

                writer.Write(GetVersion(bmdFile, "BmdOutline"));
                WriteCollection(writer, bmdFile.BmdOutlines, (w, o) => { w.Write(o.Version); WritePoints(w, o.Points); });
                WriteCollection(writer, bmdFile.TerrainOutlines, (w, o) => WritePoints(w, o.VertexList));
                WriteCollection(writer, bmdFile.LiteBuildingOutlines, (w, o) => WritePoints(w, o.VertexList));

                if (v > 4)
                {
                    writer.Write(GetVersion(bmdFile, "CameraZone"));
                    WriteCollection(writer, bmdFile.CameraZones, (w, z) => { w.Write(z.Version); WritePoints(w, z.Points); });
                }

                if (v > 7)
                {
                    WriteCollection(writer, bmdFile.CivilianDeployments, WriteCivilianDeployment);
                    WriteCollection(writer, bmdFile.CivilianShelters, WriteCivilianShelter);
                    WritePropInfos(writer, bmdFile);
                }

                if (v > 8)
                {
                    writer.Write(GetVersion(bmdFile, "VfxInfo"));
                    WriteCollection(writer, bmdFile.VfxInfos, WriteVfxInfo);

                    writer.Write(GetVersion(bmdFile, "AiHints"));
                    WriteAiHints(writer, bmdFile);
                }

                if (v > 10)
                {
                    writer.Write(GetVersion(bmdFile, "LightProbe"));
                    WriteCollection(writer, bmdFile.LightProbes, WriteLightProbeInfo);

                    writer.Write(GetVersion(bmdFile, "TerrainHole"));
                    WriteCollection(writer, bmdFile.TerrainHoles, WriteTerrainHoleInfo);

                    writer.Write(GetVersion(bmdFile, "PointLight"));
                    WriteCollection(writer, bmdFile.PointLights, WritePointLightInfo);
                }

                if (v > 14)
                {
                    writer.Write(GetVersion(bmdFile, "BuildingProjectileEmitter"));
                    WriteCollection(writer, bmdFile.BuildingProjectileEmitters, WriteBuildingProjectileEmitter);
                }

                if (v > 15)
                    WritePlayableArea(writer, bmdFile.PlayableArea);

                if (v > 16)
                {
                    writer.Write(GetVersion(bmdFile, "PolyMesh"));
                    WriteCollection(writer, bmdFile.PolyMeshes, WritePolyMeshInfo);
                }

                if (v > 17)
                {
                    writer.Write(GetVersion(bmdFile, "TerrainStencilBlendTriangle"));
                    WriteCollection(writer, bmdFile.TerrainStencilBlendTriangles, WriteTerrainStencilBlendTriangle);
                }

                if (v > 18)
                {
                    writer.Write(GetVersion(bmdFile, "SpotLight"));
                    WriteCollection(writer, bmdFile.SpotLights, WriteSpotLightInfo);
                }

                if (v > 19)
                {
                    writer.Write(GetVersion(bmdFile, "Sound"));
                    WriteCollection(writer, bmdFile.Sounds, WriteSoundInfo);
                }

                if (v > 20)
                {
                    writer.Write(GetVersion(bmdFile, "CSC"));
                    WriteCollection(writer, bmdFile.CscInfos, WriteCscInfo);
                }

                if (v > 21)
                {
                    writer.Write(GetVersion(bmdFile, "Deployment"));
                    WriteCollection(writer, bmdFile.Deployments, WriteDeployment);
                }

                if (v > 22)
                {
                    writer.Write(GetVersion(bmdFile, "BmdCachedArea"));
                    WriteCollection(writer, bmdFile.BmdCachedAreas, WriteBmdCachedArea);
                }

                if (v > 23)
                {
                    var slotVersion = GetVersion(bmdFile, "ToggleableBuildingSlot");
                    writer.Write(slotVersion);
                    WriteCollection(writer, bmdFile.ToggleableBuildingSlots, (w, s) => WriteToggleableBuildingSlot(w, s, slotVersion));
                }

                if (v > 24)
                {
                    writer.Write(GetVersion(bmdFile, "TerraindDecal"));
                    WriteCollection(writer, bmdFile.TerraindDecals, WriteTerraindDecal);
                }

                if (v > 25)
                {
                    writer.Write(GetVersion(bmdFile, "TreeListReference"));
                    WriteCollection(writer, bmdFile.TreeListReferences, (w, r) => { w.Write(r.Version); WriteString(w, r.File); WriteString(w, r.Climate); });
                    writer.Write(GetVersion(bmdFile, "GrassListReference"));
                    WriteCollection(writer, bmdFile.GrassListReferences, (w, r) => { w.Write(r.Version); WriteString(w, r.File); WriteString(w, r.Climate); });
                }

                if (v > 26)
                    WriteCollection(writer, bmdFile.WaterOutlines, (w, o) => WritePoints(w, o.VertexList));
            }

            var bytes = stream.ToArray();
            SelfCheck(bmdFile, bytes);
            return bytes;
        }

        // Self-check

        private static void SelfCheck(BmdFile source, byte[] bytes)
        {
            BmdFile reread;
            try
            {
                reread = BmdParser.Parse(bytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("BmdWriter produced bytes that BmdParser could not re-read - refusing to save. This is a bug in BmdWriter.", ex);
            }

            void CheckCount(string name, int expected, int actual)
            {
                if (expected != actual)
                    throw new InvalidOperationException($"BmdWriter self-check failed: '{name}' had {expected} item(s) before writing but {actual} after re-parsing. Refusing to save.");
            }

            CheckCount(nameof(BmdFile.BattlefieldBuildings), source.BattlefieldBuildings.Count, reread.BattlefieldBuildings.Count);
            CheckCount(nameof(BmdFile.CaptureLocationLists), source.CaptureLocationLists.Count, reread.CaptureLocationLists.Count);
            CheckCount("CaptureLocations", source.CaptureLocationLists.Sum(l => l.Locations.Count), reread.CaptureLocationLists.Sum(l => l.Locations.Count));
            CheckCount(nameof(BmdFile.GoOutlines), source.GoOutlines.Count, reread.GoOutlines.Count);
            CheckCount(nameof(BmdFile.NonTerrainOutlines), source.NonTerrainOutlines.Count, reread.NonTerrainOutlines.Count);
            CheckCount(nameof(BmdFile.ZonesTemplates), source.ZonesTemplates.Count, reread.ZonesTemplates.Count);
            CheckCount(nameof(BmdFile.BmdInfos), source.BmdInfos.Count, reread.BmdInfos.Count);
            CheckCount(nameof(BmdFile.PropInfos), source.PropInfos.Count, reread.PropInfos.Count);
            CheckCount(nameof(BmdFile.VfxInfos), source.VfxInfos.Count, reread.VfxInfos.Count);
            CheckCount(nameof(BmdFile.LightProbes), source.LightProbes.Count, reread.LightProbes.Count);
            CheckCount(nameof(BmdFile.TerrainHoles), source.TerrainHoles.Count, reread.TerrainHoles.Count);
            CheckCount(nameof(BmdFile.PointLights), source.PointLights.Count, reread.PointLights.Count);
            CheckCount(nameof(BmdFile.BuildingProjectileEmitters), source.BuildingProjectileEmitters.Count, reread.BuildingProjectileEmitters.Count);
            CheckCount(nameof(BmdFile.PolyMeshes), source.PolyMeshes.Count, reread.PolyMeshes.Count);
            CheckCount(nameof(BmdFile.SpotLights), source.SpotLights.Count, reread.SpotLights.Count);
            CheckCount(nameof(BmdFile.Sounds), source.Sounds.Count, reread.Sounds.Count);
            CheckCount(nameof(BmdFile.CscInfos), source.CscInfos.Count, reread.CscInfos.Count);
            CheckCount(nameof(BmdFile.Deployments), source.Deployments.Count, reread.Deployments.Count);
            CheckCount(nameof(BmdFile.LegacyDeploymentAreas), source.LegacyDeploymentAreas.Count, reread.LegacyDeploymentAreas.Count);
            CheckCount(nameof(BmdFile.BattlefieldBuildingFars), source.BattlefieldBuildingFars.Count, reread.BattlefieldBuildingFars.Count);
            CheckCount(nameof(BmdFile.EFLines), source.EFLines.Count, reread.EFLines.Count);
            CheckCount(nameof(BmdFile.BmdOutlines), source.BmdOutlines.Count, reread.BmdOutlines.Count);
            CheckCount(nameof(BmdFile.TerrainOutlines), source.TerrainOutlines.Count, reread.TerrainOutlines.Count);
            CheckCount(nameof(BmdFile.LiteBuildingOutlines), source.LiteBuildingOutlines.Count, reread.LiteBuildingOutlines.Count);
            CheckCount(nameof(BmdFile.CameraZones), source.CameraZones.Count, reread.CameraZones.Count);
            CheckCount(nameof(BmdFile.CivilianDeployments), source.CivilianDeployments.Count, reread.CivilianDeployments.Count);
            CheckCount(nameof(BmdFile.CivilianShelters), source.CivilianShelters.Count, reread.CivilianShelters.Count);
            CheckCount(nameof(BmdFile.TerrainStencilBlendTriangles), source.TerrainStencilBlendTriangles.Count, reread.TerrainStencilBlendTriangles.Count);
            CheckCount(nameof(BmdFile.BmdCachedAreas), source.BmdCachedAreas.Count, reread.BmdCachedAreas.Count);
            CheckCount(nameof(BmdFile.ToggleableBuildingSlots), source.ToggleableBuildingSlots.Count, reread.ToggleableBuildingSlots.Count);
            CheckCount(nameof(BmdFile.TerraindDecals), source.TerraindDecals.Count, reread.TerraindDecals.Count);
            CheckCount(nameof(BmdFile.TreeListReferences), source.TreeListReferences.Count, reread.TreeListReferences.Count);
            CheckCount(nameof(BmdFile.GrassListReferences), source.GrassListReferences.Count, reread.GrassListReferences.Count);
            CheckCount(nameof(BmdFile.WaterOutlines), source.WaterOutlines.Count, reread.WaterOutlines.Count);
            CheckCount("AiHints.Separators", source.AiHints.Separators.Count, reread.AiHints.Separators.Count);
            CheckCount("AiHints.DirectedPoints", source.AiHints.DirectedPoints.Count, reread.AiHints.DirectedPoints.Count);
            CheckCount("AiHints.PolyLines", source.AiHints.PolyLines.Count, reread.AiHints.PolyLines.Count);
            CheckCount("AiHints.PolyLinesList", source.AiHints.PolyLinesList.Count, reread.AiHints.PolyLinesList.Count);
        }

        // Section-level helpers

        private static ushort GetVersion(BmdFile bmdFile, string key) =>
            bmdFile.SectionVersions.TryGetValue(key, out var version) ? version : (ushort)0;

        private static void WriteCollection<T>(BinaryWriter writer, List<T> items, Action<BinaryWriter, T> writeItem)
        {
            writer.Write((uint)items.Count);
            foreach (var item in items)
                writeItem(writer, item);
        }

        private static void WritePoints(BinaryWriter writer, List<RmvVector2> points)
        {
            writer.Write((uint)points.Count);
            foreach (var p in points)
                WriteVector2(writer, p);
        }

        private static void WriteUInt32List(BinaryWriter writer, List<uint> values)
        {
            writer.Write((uint)values.Count);
            foreach (var value in values)
                writer.Write(value);
        }

        // Item writers (mirror BmdParser's Read* methods)

        private static void WriteLegacyDeploymentArea(BinaryWriter writer, LegacyDeploymentArea area, ushort managerVersion)
        {
            writer.Write(area.TypeX);
            writer.Write(area.TypeY);
            WriteCollection(writer, area.Collections, (w, collection) =>
            {
                WriteLegacyDeploymentZones(w, collection.Alliance0, managerVersion);
                WriteLegacyDeploymentZones(w, collection.Alliance1, managerVersion);
            });
        }

        private static void WriteLegacyDeploymentZones(BinaryWriter writer, List<LegacyDeploymentZone> zones, ushort managerVersion)
        {
            WriteCollection(writer, zones, (w, zone) =>
            {
                WriteCollection(w, zone.Boundaries, (w2, boundary) => { WriteString(w2, boundary.Type); WritePoints(w2, boundary.Points); });
                WriteString(w, zone.Category);
                w.Write(zone.Orientation);
                w.Write(Bool(zone.SnapFacing));
                if (managerVersion >= 2)
                    w.Write(zone.Id);
            });
        }

        private static void WriteBattlefieldBuilding(BinaryWriter writer, BattlefieldBuilding building)
        {
            writer.Write(building.Version);
            WriteString(writer, building.BuildingId);

            if (building.Version > 10)
                writer.Write(building.ParentId);
            else if (building.Version > 6)
                writer.Write((short)building.ParentId);

            if (building.Version > 4)
                WriteString(writer, building.BuildingKey);

            WriteString(writer, building.PositionType);

            if (building.Version < 6)
            {
                // Matrix.Decompose undoes the parser's scale * rotation * translation composition.
                building.Transform.Decompose(out var scale, out var rotation, out var translation);
                WriteVector3(writer, translation);
                writer.Write(rotation.X);
                writer.Write(rotation.Y);
                writer.Write(rotation.Z);
                writer.Write(rotation.W);
                WriteVector3(writer, scale);
            }
            else
            {
                WriteRowMajorMatrix(writer, building.Transform, is4x4: false);
            }

            if (building.Version > 3)
                WriteBuildingProperties(writer, building.Properties);
            else
                WriteLegacyBuildingProperties(writer, building.Properties, building.Version);

            if (building.Version > 7)
                WriteString(writer, building.HeightMode);
            if (building.Version > 9)
                writer.Write(building.Uid);
        }

        private static void WriteBuildingProperties(BinaryWriter writer, BuildingProperties properties)
        {
            writer.Write(properties.Version);
            if (properties.Version > 1)
                WriteString(writer, properties.BuildingId);
            writer.Write(properties.StartingDamageUnary);
            writer.Write(Bool(properties.OnFire));
            writer.Write(Bool(properties.StartDisabled));
            writer.Write(Bool(properties.WeakPoint));
            writer.Write(Bool(properties.AiBreachable));
            writer.Write(Bool(properties.Indestructible));
            writer.Write(Bool(properties.Dockable));
            writer.Write(Bool(properties.Toggleable));
            writer.Write(Bool(properties.Lite));
            if (properties.Version is > 2 and < 8)
                writer.Write(Bool(properties.ClampToSurface));
            if (properties.Version > 3)
                writer.Write(Bool(properties.CastShadows));
            if (properties.Version > 4)
                writer.Write(Bool(properties.KeyBuilding));
            if (properties.Version > 5)
                writer.Write(Bool(properties.KeyBuildingUseFort));
            if (properties.Version > 6)
                writer.Write(Bool(properties.IsPropInOutfield));
            if (properties.Version > 8)
                writer.Write(Bool(properties.SettlementLevelConfigurable));
            if (properties.Version > 9)
                writer.Write(Bool(properties.HideTooltip));
            if (properties.Version > 10)
                writer.Write(Bool(properties.IncludeInFog));
            if (properties.Version > 11)
                writer.Write(properties.BlendEdges);
            if (properties.Version > 12)
                writer.Write(Bool(properties.ReceiveDecals));
        }

        private static void WriteLegacyBuildingProperties(BinaryWriter writer, BuildingProperties properties, ushort buildingVersion)
        {
            writer.Write(properties.StartingDamageUnary);
            writer.Write(Bool(properties.OnFire));
            writer.Write(Bool(properties.StartDisabled));
            writer.Write(Bool(properties.WeakPoint));
            writer.Write(Bool(properties.AiBreachable));
            writer.Write(Bool(properties.Indestructible));
            writer.Write(Bool(properties.Dockable));
            if (buildingVersion > 1)
                writer.Write(Bool(properties.Toggleable));
            if (buildingVersion > 2)
                writer.Write(Bool(properties.Lite));
        }

        private static void WriteCaptureLocation(BinaryWriter writer, CaptureLocation location, ushort listVersion)
        {
            WriteVector2(writer, location.Location);
            writer.Write(location.Radius);
            writer.Write(location.ValidForMinNumPlayers);
            writer.Write(location.ValidForMaxNumPlayers);
            WriteString(writer, location.CapturePointType);
            if (listVersion > 2)
                WriteString(writer, location.RestoreType);
            WritePoints(writer, location.LocationPoints);
            if (listVersion == 1)
                WriteUInt32List(writer, location.BuildingIndices);
            WriteString(writer, location.DatabaseKey);
            WriteVector2(writer, location.FlagFacing);
            if (listVersion >= 4)
                writer.Write(Bool(location.DestroyBuildingOnCapture));
            if (listVersion >= 6)
                writer.Write(Bool(location.DisableBuildingAbilitiesWhenNoOriginalOwner));
            if (listVersion >= 5)
                writer.Write(Bool(location.AbilitiesAffectGlobally));
            if (listVersion > 1)
                WriteBuildingLinks(writer, location.BuildingLinks);
            if (listVersion >= 7)
                WriteUInt32List(writer, location.ToggleSlotsLinks);
            if (listVersion >= 8)
                WriteUInt32List(writer, location.AiHintsLinks);
            if (listVersion >= 9)
                WriteString(writer, location.ScriptId);
            if (listVersion >= 10)
                writer.Write(Bool(location.IsTimeBased));
            if (listVersion > 11)
                WriteString(writer, location.InitialOwnerType);
        }

        private static void WriteBuildingLinks(BinaryWriter writer, List<BuildingLink> links)
        {
            writer.Write((uint)links.Count);
            foreach (var link in links)
            {
                writer.Write(link.Version);
                writer.Write(link.BuildingIndex);
                writer.Write(link.PrefabIndex);
                WriteString(writer, link.PrefabBuildingKey);
                if (link.Version >= 2)
                    writer.Write(link.Uid);
                if (link.Version >= 3)
                    writer.Write(link.PrefabUid);
            }
        }

        private static void WriteEFLine(BinaryWriter writer, EFLine line)
        {
            WriteString(writer, line.Label);
            WriteVector3(writer, line.End1);
            WriteVector3(writer, line.End2);
            WriteVector3(writer, line.Direction);
            WriteString(writer, line.Purpose);
        }

        private static void WriteZonesTemplate(BinaryWriter writer, ZonesTemplate template)
        {
            WritePoints(writer, template.Outline);
            WriteString(writer, template.ZoneName);
            WriteString(writer, template.EntityFormationTemplateName);

            writer.Write((uint)template.Lines.Count);
            foreach (var line in template.Lines)
            {
                WriteString(writer, line.Label);
                WriteVector3(writer, line.Start);
                WriteVector3(writer, line.End);
                WriteString(writer, line.Purpose);
                writer.Write(line.Orientation);
            }

            WriteRowMajorMatrix(writer, template.Transform, is4x4: true);
        }

        private static void WriteCivilianDeployment(BinaryWriter writer, CivilianDeployment deployment)
        {
            writer.Write(deployment.Version);
            WriteString(writer, deployment.CivilianType);
            WriteString(writer, deployment.CivilianAmount);
            WritePoints(writer, deployment.Outline);
        }

        private static void WriteCivilianShelter(BinaryWriter writer, CivilianShelter shelter)
        {
            writer.Write(shelter.Version);
            WriteVector2(writer, shelter.Position);
            WriteVector2(writer, shelter.Entrance);
            writer.Write(shelter.CivilianDeploymentLink);
        }

        private static void WriteTerrainStencilBlendTriangle(BinaryWriter writer, TerrainStencilBlendTriangle triangle)
        {
            writer.Write(triangle.Version);
            WriteVector3(writer, triangle.Position0);
            WriteVector3(writer, triangle.Position1);
            WriteVector3(writer, triangle.Position2);
            if (triangle.Version >= 2)
                WriteString(writer, triangle.HeightMode);
        }

        private static void WriteBmdCachedArea(BinaryWriter writer, BmdCachedArea area)
        {
            writer.Write(area.Version);
            WriteString(writer, area.Name);
            for (var i = 0; i < 4; i++)
                writer.Write(area.Aabb[i]);
            if (area.Version >= 6)
            {
                WriteString(writer, area.BattleType);
                WriteString(writer, area.DefendingFactionRestriction);
            }
            if (area.Version >= 5)
            {
                var flags = area.ValidLocationFlags;
                writer.Write(flags.Version);
                writer.Write(Bool(flags.ValidNorth));
                writer.Write(Bool(flags.ValidSouth));
                writer.Write(Bool(flags.ValidEast));
                writer.Write(Bool(flags.ValidWest));
            }
        }

        private static void WriteToggleableBuildingSlot(BinaryWriter writer, ToggleableBuildingSlot slot, ushort listVersion)
        {
            if (listVersion > 1)
                WriteString(writer, slot.ToggleableSlotType);
            WriteBuildingLinks(writer, slot.BuildingLinks);
            if (listVersion >= 3)
            {
                WriteCollection(writer, slot.CompositeScenes, WriteCscInfo);
                WriteCollection(writer, slot.Outlines, WritePoints);
            }
            if (listVersion >= 4)
                WriteString(writer, slot.ScriptId);
            if (listVersion >= 5)
                WriteString(writer, slot.MapBarrierRecordKey);
            if (listVersion == 6)
                writer.Write(Bool(slot.MeleeAttackAllowed));
            if (listVersion >= 7)
                writer.Write(Bool(slot.GroundMeleeAttackAllowed));
        }

        private static void WriteTerraindDecal(BinaryWriter writer, TerraindDecal decal)
        {
            writer.Write(decal.Version);
            writer.Write(decal.SplineVersion);
            WriteVector2(writer, decal.VertexSpacing);
            writer.Write(decal.UvScale);
            writer.Write((uint)decal.Splines.Count);
            foreach (var spline in decal.Splines)
            {
                writer.Write(spline.Version);
                writer.Write(spline.StartWidth);
                writer.Write(spline.EndWidth);
                WriteVector3(writer, spline.Cp0);
                WriteVector3(writer, spline.Cp1);
                WriteVector3(writer, spline.Cp2);
                WriteVector3(writer, spline.Cp3);
            }
            WriteString(writer, decal.Material);
            if (decal.Version >= 2)
                WriteRowMajorMatrix(writer, decal.Transform, is4x4: false);
        }

        private static void WriteBmdInfo(BinaryWriter writer, BmdInfo bmd)
        {
            writer.Write(bmd.Version);
            WriteString(writer, bmd.BmdString);
            WriteRowMajorMatrix(writer, bmd.Transform, is4x4: true);
            if (bmd.Version > 1)
                WriteCollection(writer, bmd.PropertyOverrides, WriteBuildingProperties);
            if (bmd.Version > 6)
                WriteCultureMask(writer, bmd.CampaignTypeMask);
            else if (bmd.Version > 2)
                WriteCultureMask(writer, bmd.CampaignTypeMask, 4);
            if (bmd.Version > 3)
                WriteString(writer, bmd.RegionString);
            if (bmd.Version is > 4 and < 8)
                writer.Write(Bool(bmd.ClampToSurface));
            if (bmd.Version > 5)
                WriteString(writer, bmd.HeightMode);
            if (bmd.Version > 8)
                WriteFixedBytes(writer, bmd.Uid, 8);
        }

        private static void WritePropInfos(BinaryWriter writer, BmdFile bmdFile)
        {
            var propVersion = GetVersion(bmdFile, "Prop");
            writer.Write(propVersion);

            var propsList = new List<string>();
            if (propVersion > 1)
            {
                // Keep the original string table so unedited files round-trip exactly; append any path a
                // prop now references that isn't in it.
                propsList.AddRange(bmdFile.Props);
                var known = new HashSet<string>(propsList, StringComparer.Ordinal);
                foreach (var prop in bmdFile.PropInfos)
                {
                    if (prop.PropInfoVersion > 12 && known.Add(prop.Rmv2Path))
                        propsList.Add(prop.Rmv2Path);
                }

                writer.Write((uint)propsList.Count);
                foreach (var path in propsList)
                    WriteString(writer, path);
            }

            var propIndexLookup = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < propsList.Count; i++)
                propIndexLookup[propsList[i]] = i;

            writer.Write((uint)bmdFile.PropInfos.Count);
            foreach (var prop in bmdFile.PropInfos)
                WritePropInfo(writer, prop, propsList, propIndexLookup);
        }

        private static void WritePropInfo(BinaryWriter writer, PropInfo prop, List<string> propsList, Dictionary<string, int> propIndexLookup)
        {
            writer.Write(prop.PropInfoVersion);
            if (prop.PropInfoVersion <= 12)
            {
                WriteString(writer, prop.Rmv2Path);
            }
            else
            {
                // The table can hold duplicate paths, so keep the prop's own index while it still points at
                // the right string.
                var index = prop.PropIndex is { } original && original < propsList.Count && propsList[original] == prop.Rmv2Path
                    ? original
                    : propIndexLookup[prop.Rmv2Path];
                writer.Write((uint)index);
            }

            WriteRowMajorMatrix(writer, prop.Transform, is4x4: false);

            if (prop.PropInfoVersion < 4)
            {
                writer.Write(Bool(prop.Flags.ClampToSurface));
                writer.Write(Bool(prop.IsDecal));
                writer.Write(Bool(prop.LogicalDecal));
                if (prop.PropInfoVersion <= 2)
                {
                    writer.Write(prop.DecalParallax);
                    WriteVector3(writer, prop.DecalScale);
                    writer.Write(Bool(prop.DecalTiled));
                }
                writer.Write(Bool(prop.Flags.AllowInOutfield));
                writer.Write(Bool(prop.IsFauna));
                writer.Write(Bool(prop.VisibleInsideSnowRegion));
                writer.Write(Bool(prop.VisibleOutsideSnowRegion));
                writer.Write(Bool(prop.VisibleInsideDestructionRegion));
                writer.Write(Bool(prop.VisibleOutsideDestructionRegion));
                writer.Write(Bool(prop.Flags.SeasonSpring));
                writer.Write(Bool(prop.Flags.SeasonSummer));
                writer.Write(Bool(prop.Flags.SeasonAutumn));
                writer.Write(Bool(prop.Flags.SeasonWinter));
                if (prop.PropInfoVersion >= 2)
                    writer.Write(Bool(prop.Animated));
                if (prop.PropInfoVersion >= 3)
                {
                    writer.Write(prop.DecalParallaxScale);
                    writer.Write(prop.DecalTiling);
                    // Keep vanilla's junk byte unless the bool has been edited since.
                    writer.Write(prop.DecalOverrideGbufferNormalRawByte is { } raw && prop.DecalOverrideGbufferNormal
                        ? raw
                        : Bool(prop.DecalOverrideGbufferNormal));
                }
                return;
            }

            writer.Write(Bool(prop.IsDecal));
            writer.Write(Bool(prop.LogicalDecal));
            writer.Write(Bool(prop.IsFauna));
            writer.Write(Bool(prop.VisibleInsideSnowRegion));
            writer.Write(Bool(prop.VisibleOutsideSnowRegion));
            writer.Write(Bool(prop.VisibleInsideDestructionRegion));
            writer.Write(Bool(prop.VisibleOutsideDestructionRegion));
            writer.Write(Bool(prop.Animated));
            writer.Write(prop.DecalParallaxScale);
            writer.Write(prop.DecalTiling);
            writer.Write(Bool(prop.DecalOverrideGbufferNormal));

            WriteBmdComponentFlags(writer, prop.Flags);

            if (prop.PropInfoVersion > 4)
                writer.Write(Bool(prop.VisibleInShroud));
            if (prop.PropInfoVersion > 5)
            {
                writer.Write(Bool(prop.ApplyToTerrain));
                writer.Write(Bool(prop.ApplyToPropsOrReceiveDecal));
                writer.Write(Bool(prop.RenderAboveSnow));
            }

            if (prop.PropInfoVersion > 7)
                WriteString(writer, prop.HeightMode);

            if (prop.PropInfoVersion > 15)
                WriteFixedBytes(writer, prop.CultureMask, 8);
            else if (prop.PropInfoVersion > 10)
                writer.Write(prop.CultureMask, 0, 4);

            if (prop.PropInfoVersion == 9)
                writer.Write(Bool(prop.DecalApplyToRigids));
            if (prop.PropInfoVersion > 11)
                writer.Write(Bool(prop.CastsShadow));
            if (prop.PropInfoVersion > 13)
                writer.Write(Bool(prop.NoCulling));
            if (prop.PropInfoVersion > 14)
                writer.Write(Bool(prop.HasHeightPatch));
            if (prop.PropInfoVersion > 16)
                writer.Write(Bool(prop.ApplyHeightPatch));
            if (prop.PropInfoVersion > 18)
                writer.Write(Bool(prop.IncludeInFog));
            if (prop.PropInfoVersion > 19)
                writer.Write(Bool(prop.VisibleWithoutShroud));

            if (prop.PropInfoVersion is 21 or 22)
                writer.Write(Bool(prop.VisibleInTacticalView));
            if (prop.PropInfoVersion == 22)
                writer.Write(Bool(prop.VisibleInTacticalViewOnly));

            if (prop.PropInfoVersion > 23)
                writer.Write(Bool(prop.UseDynamicShadows));
            if (prop.PropInfoVersion > 24)
                writer.Write(Bool(prop.UsesTerrainVertexOffset));

            if (prop.PropInfoVersion > 25)
                writer.Write(prop.BlendEdges);
            if (prop.PropInfoVersion > 26)
                writer.Write(prop.DecalFadeStart);
            if (prop.PropInfoVersion > 27)
            {
                writer.Write(prop.DecalAngleThreshold);
                writer.Write(Bool(prop.DecalMirrorX));
                writer.Write(Bool(prop.DecalMirrorZ));
            }
            if (prop.PropInfoVersion > 28)
                writer.Write(prop.DecalLayer);
            if (prop.PropInfoVersion > 29)
                writer.Write(Bool(prop.DecalTilingAlpha));
        }

        private static void WriteVfxInfo(BinaryWriter writer, VfxInfo vfx)
        {
            writer.Write(vfx.VfxInfoVersion);
            WriteString(writer, vfx.VfxString);
            WriteRowMajorMatrix(writer, vfx.Transform, is4x4: false);
            writer.Write(vfx.EmissionRate);
            WriteString(writer, vfx.InstanceName);

            if (vfx.VfxInfoVersion == 1)
            {
                writer.Write(Bool(vfx.Autoplay));
                writer.Write(vfx.Scale);
                writer.Write(Bool(vfx.Flags.AllowInOutfield));
            }
            else
            {
                WriteBmdComponentFlags(writer, vfx.Flags);
            }

            if (vfx.VfxInfoVersion > 2)
                WriteString(writer, vfx.HeightMode);
            if (vfx.VfxInfoVersion > 3)
            {
                if (vfx.VfxInfoVersion > 6)
                    WriteFixedBytes(writer, vfx.CultureMask, 8);
                else
                    writer.Write(vfx.CultureMask, 0, 4);
            }
            if (vfx.VfxInfoVersion > 4)
            {
                writer.Write(Bool(vfx.Autoplay));
                writer.Write(Bool(vfx.VisibleInShroud));
            }

            if (vfx.VfxInfoVersion == 8)
                writer.Write((short)vfx.ParentId);
            else if (vfx.VfxInfoVersion > 8)
                writer.Write(vfx.ParentId);

            if (vfx.VfxInfoVersion > 9)
                writer.Write(Bool(vfx.VisibleWithoutShroud));
            if (vfx.VfxInfoVersion > 10)
                writer.Write(Bool(vfx.NoCulling));
        }

        private static void WriteAiHints(BinaryWriter writer, BmdFile bmdFile)
        {
            var aiHints = bmdFile.AiHints;

            writer.Write(GetVersion(bmdFile, "AiHints.Separators"));
            WriteCollection(writer, aiHints.Separators, (w, s) => { w.Write(s.Version); WriteString(w, s.Type); WritePoints(w, s.Points); });

            writer.Write(GetVersion(bmdFile, "AiHints.DirectedPoints"));
            WriteCollection(writer, aiHints.DirectedPoints, (w, p) =>
            {
                w.Write(p.Version);
                WriteString(w, p.Type);
                WriteVector2(w, p.Position);
                WriteVector2(w, p.Direction);
            });

            writer.Write(GetVersion(bmdFile, "AiHints.PolyLines"));
            writer.Write((uint)aiHints.PolyLines.Count);
            foreach (var polyLine in aiHints.PolyLines)
            {
                writer.Write(polyLine.Version);
                WriteString(writer, polyLine.Type);
                writer.Write((uint)polyLine.Points.Count);
                foreach (var p in polyLine.Points)
                {
                    writer.Write(p.X);
                    writer.Write(p.Y);
                }
                if (polyLine.Version > 1)
                    WriteString(writer, polyLine.ScriptId);
                if (polyLine.Version > 2)
                    writer.Write(Bool(polyLine.OnlyVanguard));
                if (polyLine.Version > 3)
                {
                    writer.Write(Bool(polyLine.OnlyDeployWhenClear));
                    writer.Write(Bool(polyLine.SpawnVfx));
                }
            }

            writer.Write(GetVersion(bmdFile, "AiHints.PolyLinesList"));
            writer.Write((uint)aiHints.PolyLinesList.Count);
            foreach (var polyLineList in aiHints.PolyLinesList)
            {
                writer.Write(polyLineList.Version);
                WriteString(writer, polyLineList.Type);
                if (polyLineList.Version >= 2)
                    writer.Write(polyLineList.District);
                writer.Write((uint)polyLineList.PolygonList.Count);
                foreach (var polygon in polyLineList.PolygonList)
                    WritePoints(writer, polygon.Points);
            }
        }

        private static void WriteLightProbeInfo(BinaryWriter writer, LightProbeInfo probe)
        {
            writer.Write(probe.Version);
            WriteVector3(writer, probe.Position);
            writer.Write(probe.OuterRadius);

            if (probe.Version > 2)
            {
                writer.Write(probe.InnerRadius);
                writer.Write(Bool(probe.IsCylinder));
            }

            writer.Write(Bool(probe.Primary));
            if (probe.Version > 1)
                WriteString(writer, probe.HeightMode);
        }

        private static void WriteTerrainHoleInfo(BinaryWriter writer, TerrainHoleTriangleInfo hole)
        {
            writer.Write(hole.TerrainHoleVersion);
            WriteVector3(writer, hole.FirstVert);
            WriteVector3(writer, hole.SecondVert);
            WriteVector3(writer, hole.ThirdVert);
            if (hole.TerrainHoleVersion > 1)
                WriteString(writer, hole.HeightMode);

            if (hole.TerrainHoleVersion > 2)
                WriteBmdComponentFlags(writer, hole.Flags);
        }

        private static void WritePointLightInfo(BinaryWriter writer, PointLightInfo light)
        {
            writer.Write(light.PointLightInfoVersion);
            WriteVector3(writer, light.Position);
            writer.Write(light.Radius);
            writer.Write(light.Red);
            writer.Write(light.Green);
            writer.Write(light.Blue);
            writer.Write(light.ColorScale);
            writer.Write(light.AnimationTypeEnum);
            writer.Write(light.AnimationSpeedScale1);
            writer.Write(light.AnimationSpeedScale2);
            writer.Write(light.ColorMin);
            writer.Write(light.RandomOffset);
            WriteString(writer, light.FalloffType);
            writer.Write(light.LFRelative);

            if (light.PointLightInfoVersion > 1)
                WriteString(writer, light.HeightMode);
            if (light.PointLightInfoVersion > 2)
                writer.Write(Bool(light.LightProbeOnly));
            if (light.PointLightInfoVersion > 3)
            {
                if (light.PointLightInfoVersion > 5)
                    writer.Write(light.PdlcMask);
                else
                    writer.Write((uint)light.PdlcMask);
            }
            if (light.PointLightInfoVersion > 6)
                WriteBmdComponentFlags(writer, light.Flags);
        }

        private static void WriteBuildingProjectileEmitter(BinaryWriter writer, BuildingProjectileEmitter emitter)
        {
            writer.Write(emitter.BuildingProjectileEmitterVersion);
            WriteVector3(writer, emitter.Location);
            writer.Write(emitter.Rotation[0]);
            writer.Write(emitter.Rotation[1]);
            writer.Write(emitter.Rotation[2]);
            writer.Write(emitter.BuildingIndex);
            WriteString(writer, emitter.HeightMode);

            if (emitter.BuildingProjectileEmitterVersion > 2)
                WriteString(writer, emitter.SpecializedBuildingProjectileEmitterKey);
        }

        private static void WritePlayableArea(BinaryWriter writer, PlayableArea area)
        {
            writer.Write(area.PlayableAreaVersion);
            for (var i = 0; i < 4; i++)
                writer.Write(area.BoundingBox[i]);
            writer.Write(Bool(area.HasBeenSet));

            if (area.PlayableAreaVersion > 1)
            {
                if (area.PlayableAreaVersion > 2)
                    writer.Write(area.FlagVersion);
                writer.Write(Bool(area.ValidNorth));
                writer.Write(Bool(area.ValidSouth));
                writer.Write(Bool(area.ValidEast));
                writer.Write(Bool(area.ValidWest));
            }
        }

        private static void WritePolyMeshInfo(BinaryWriter writer, PolyMeshInfo mesh)
        {
            writer.Write(mesh.PolyMeshVersion);
            writer.Write((uint)mesh.VertexList.Length);
            foreach (var v in mesh.VertexList)
                WriteVector3(writer, v);

            writer.Write((uint)mesh.TriangleList.Length);
            foreach (var t in mesh.TriangleList)
                writer.Write(t);

            WriteString(writer, mesh.MaterialString);
            WriteString(writer, mesh.HeightMode);

            if (mesh.PolyMeshVersion > 2)
                WriteBmdComponentFlags(writer, mesh.Flags);
            if (mesh.PolyMeshVersion > 3)
            {
                WriteRowMajorMatrix(writer, mesh.Transform, is4x4: false);
                writer.Write(Bool(mesh.VisibleInsideSnowRegion));
                writer.Write(Bool(mesh.VisibleOutsideSnowRegion));
                writer.Write(Bool(mesh.VisibleInsideDestructionRegion));
                writer.Write(Bool(mesh.VisibleOutsideDestructionRegion));
                writer.Write(Bool(mesh.VisibleInShroud));
                writer.Write(Bool(mesh.VisibleWithoutShroud));
            }
        }

        private static void WriteSpotLightInfo(BinaryWriter writer, SpotLightInfo light)
        {
            writer.Write(light.Version);
            WriteVector3(writer, light.Position);
            writer.Write(light.QuartX);
            writer.Write(light.QuartY);
            writer.Write(light.QuartZ);
            writer.Write(light.QuartW);
            writer.Write(light.Length);
            writer.Write(light.InnerAngleRadians);
            writer.Write(light.OuterAngleRadians);
            writer.Write(light.IntensityRed);
            writer.Write(light.IntensityGreen);
            writer.Write(light.IntensityBlue);
            writer.Write(light.Falloff);
            WriteString(writer, light.Gobo);
            if (light.Version > 1)
                writer.Write(Bool(light.Volumetric));
            if (light.Version > 2)
                WriteString(writer, light.HeightMode);
            if (light.Version > 6)
                writer.Write(light.PdlcMask);
            else if (light.Version > 3)
                writer.Write((uint)light.PdlcMask);

            if (light.Version > 7)
                WriteBmdComponentFlags(writer, light.Flags);
        }

        private static void WriteSoundInfo(BinaryWriter writer, SoundInfo sound)
        {
            writer.Write(sound.Version);
            WriteString(writer, sound.SoundString);
            WriteString(writer, sound.TypeString);

            writer.Write((uint)sound.CoordList.Length);
            foreach (var c in sound.CoordList)
                WriteVector3(writer, c);

            writer.Write(sound.InnerRadius);
            writer.Write(sound.OuterRadius);

            WriteVector3(writer, sound.InnerCubeBoundingBox.Min);
            WriteVector3(writer, sound.InnerCubeBoundingBox.Max);
            WriteVector3(writer, sound.OuterCubeBoundingBox.Min);
            WriteVector3(writer, sound.OuterCubeBoundingBox.Max);

            if (sound.Version > 1)
            {
                writer.Write((uint)sound.RiverNodeList.Length);
                foreach (var node in sound.RiverNodeList)
                {
                    writer.Write(node.Version);
                    WriteVector3(writer, node.Position);
                    writer.Write(node.Width);
                    writer.Write(node.FlowSpeed);
                }
            }
            if (sound.Version > 2)
                writer.Write(sound.ClampToSurface);
            if (sound.Version > 3)
                WriteString(writer, sound.HeightMode);
            if (sound.Version > 9)
                writer.Write(sound.CampaignTypeMask);
            else if (sound.Version > 4)
                writer.Write((uint)sound.CampaignTypeMask);
            if (sound.Version > 6)
                WriteCultureMask(writer, sound.CultureMask);
            else if (sound.Version > 5)
                WriteCultureMask(writer, sound.CultureMask, 4);
            if (sound.Version > 7)
            {
                WriteVector3(writer, sound.DirectionVector);
                WriteVector3(writer, sound.UpVector);
            }
            if (sound.Version > 8)
                WriteString(writer, sound.Scope);
        }

        private static void WriteCscInfo(BinaryWriter writer, CscInfo csc)
        {
            writer.Write(csc.Version);
            WriteRowMajorMatrix(writer, csc.Transform, is4x4: false);
            WriteString(writer, csc.SceneFile);
            if (csc.Version > 1)
                WriteString(writer, csc.HeightMode);
            if (csc.Version > 6)
                writer.Write(csc.PdlcMask);
            else if (csc.Version > 2)
                writer.Write((uint)csc.PdlcMask);
            writer.Write(Bool(csc.Autoplay));
            if (csc.Version > 4)
                writer.Write(Bool(csc.VisibleInShroud));
            if (csc.Version > 5)
                writer.Write(Bool(csc.NoCulling));
            if (csc.Version > 7)
                WriteString(writer, csc.ScriptId);
            if (csc.Version > 8)
                WriteString(writer, csc.ParentScriptId);
            if (csc.Version > 9)
                writer.Write(Bool(csc.VisibleWithoutShroud));
            if (csc.Version > 10)
            {
                writer.Write(Bool(csc.VisibleInTacticalView));
                writer.Write(Bool(csc.VisibleInTacticalViewOnly));
            }
            if (csc.Version > 11)
            {
                writer.Write(Bool(csc.HoldFirst));
                writer.Write(Bool(csc.HoldLast));
            }
        }

        private static void WriteDeployment(BinaryWriter writer, Deployment deployment)
        {
            writer.Write(deployment.Version);
            WriteString(writer, deployment.Category);
            writer.Write((uint)deployment.DeploymentZones.Count);
            foreach (var zone in deployment.DeploymentZones)
                WriteDeploymentZone(writer, zone);
        }

        private static void WriteDeploymentZone(BinaryWriter writer, DeploymentZone zone)
        {
            writer.Write(zone.Version);
            writer.Write((uint)zone.DeploymentZoneRegions.Count);
            foreach (var region in zone.DeploymentZoneRegions)
                WriteDeploymentZoneRegion(writer, region);
        }

        private static void WriteDeploymentZoneRegion(BinaryWriter writer, DeploymentZoneRegion region)
        {
            writer.Write(region.Version);
            writer.Write((uint)region.Boundaries.Count);
            foreach (var boundary in region.Boundaries)
                WriteBoundary(writer, boundary);

            writer.Write(region.Orientation);
            writer.Write(region.SnapFacing);
            writer.Write(region.Id);
        }

        private static void WriteBoundary(BinaryWriter writer, Boundary boundary)
        {
            writer.Write(boundary.Version);
            WriteString(writer, boundary.BoundaryType);
            writer.Write((uint)boundary.PointList.Count);
            foreach (var p in boundary.PointList)
            {
                writer.Write(p.X);
                writer.Write(p.Y);
            }
        }

        // Primitive helpers (mirror BmdParser's Read helpers)

        private static byte Bool(bool b) => (byte)(b ? 1 : 0);

        private static void WriteString(BinaryWriter writer, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s ?? string.Empty);
            if (bytes.Length > ushort.MaxValue)
                throw new InvalidOperationException($"String too long to write as a BMD string ({bytes.Length} bytes, max {ushort.MaxValue}): '{s}'");

            writer.Write((ushort)bytes.Length);
            if (bytes.Length > 0)
                writer.Write(bytes);
        }

        private static void WriteFixedBytes(BinaryWriter writer, byte[] bytes, int expectedLength)
        {
            if (bytes.Length != expectedLength)
                throw new InvalidOperationException($"Expected exactly {expectedLength} bytes but got {bytes.Length}.");
            writer.Write(bytes);
        }

        private static void WriteVector2(BinaryWriter writer, RmvVector2 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
        }

        private static void WriteVector3(BinaryWriter writer, RmvVector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        private static void WriteRowMajorMatrix(BinaryWriter writer, Matrix m, bool is4x4)
        {
            writer.Write(m.M11);
            writer.Write(m.M12);
            writer.Write(m.M13);
            if (is4x4)
                writer.Write(m.M14);

            writer.Write(m.M21);
            writer.Write(m.M22);
            writer.Write(m.M23);
            if (is4x4)
                writer.Write(m.M24);

            writer.Write(m.M31);
            writer.Write(m.M32);
            writer.Write(m.M33);
            if (is4x4)
                writer.Write(m.M34);

            writer.Write(m.M41);
            writer.Write(m.M42);
            writer.Write(m.M43);
            if (is4x4)
                writer.Write(m.M44);
        }

        private static void WriteBmdComponentFlags(BinaryWriter writer, BmdComponentFlags flags)
        {
            writer.Write(flags.FlagVersion);
            writer.Write(Bool(flags.AllowInOutfield));

            if (flags.FlagVersion < 3)
                writer.Write(Bool(flags.ClampToSurface));
            writer.Write(Bool(flags.ClampToWaterSurface));

            writer.Write(Bool(flags.SeasonSpring));
            writer.Write(Bool(flags.SeasonSummer));
            writer.Write(Bool(flags.SeasonAutumn));
            writer.Write(Bool(flags.SeasonWinter));

            if (flags.FlagVersion > 3)
            {
                writer.Write(Bool(flags.VisibleInTactical));
                writer.Write(Bool(flags.OnlyVisibleInTactical));
            }
        }

        private static void WriteCultureMask(BinaryWriter writer, CultureMask mask, int size = 8)
        {
            if (mask.RawBytes is { Length: 8 } raw)
            {
                writer.Write(raw, 0, size);
                return;
            }
            var bytes = new byte[8];

            if (mask.CultMaskBase) bytes[0] |= 0x01;
            if (mask.CultMaskBst) bytes[0] |= 0x40;
            if (mask.CultMaskBrt) bytes[0] |= 0x80;

            if (mask.CultMaskChs) bytes[1] |= 0x01;
            if (mask.CultMaskDwf) bytes[1] |= 0x02;
            if (mask.CultMaskEmp) bytes[1] |= 0x04;
            if (mask.CultMaskGrn) bytes[1] |= 0x08;
            if (mask.CultMaskVmp) bytes[1] |= 0x10;
            if (mask.CultMaskWef) bytes[1] |= 0x20;

            if (mask.CultMaskDef) bytes[2] |= 0x02;
            if (mask.CultMaskHef) bytes[2] |= 0x04;
            if (mask.CultMaskLzd) bytes[2] |= 0x08;
            if (mask.CultMaskSkv) bytes[2] |= 0x10;
            if (mask.CultMaskTmb) bytes[2] |= 0x20;
            if (mask.CultMaskRogue) bytes[2] |= 0x40;
            if (mask.CultMaskKsl) bytes[2] |= 0x80;

            if (mask.CultMaskOgr) bytes[3] |= 0x01;
            if (mask.CultMaskCst) bytes[3] |= 0x02;
            if (mask.CultMaskKho) bytes[3] |= 0x08;
            if (mask.CultMaskTze) bytes[3] |= 0x10;
            if (mask.CultMaskNur) bytes[3] |= 0x20;
            if (mask.CultMaskSla) bytes[3] |= 0x40;
            if (mask.CultMaskDae) bytes[3] |= 0x80;

            if (mask.CultMaskCth) bytes[4] |= 0x01;
            if (mask.CultMaskNor) bytes[4] |= 0x02;
            if (mask.CultMaskChd) bytes[4] |= 0x04;
            if (mask.CultMaskNag) bytes[4] |= 0x08;

            writer.Write(bytes, 0, size);
        }
    }
}
