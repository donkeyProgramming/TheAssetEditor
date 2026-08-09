using System.Text;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Bmd
{
    /// <summary>
    /// Serializes a <see cref="BmdFile"/> back to FASTBIN0 bytes, mirroring <see cref="BmdParser"/>
    /// section-by-section in the exact same order/<see cref="FastBinHeader.FastBinVersion"/> gates.
    /// Writes the WHOLE file, not just the categories the editor exposes for editing - every other
    /// section (BattlefieldBuildings, Deployments, PlayableArea, AiHints, ...) is written back from
    /// whatever BmdParser stored, so saving an unedited file reproduces it exactly and saving an
    /// edited one only changes what was actually touched.
    ///
    /// A handful of sections BmdParser reads bytes for and then discards without storing anywhere
    /// (see the guards below, e.g. <see cref="BmdInfo.PropertyOverrides"/> == 1, PropInfo/BmdInfo's
    /// oldest versions, CaptureLocation version &gt; 2) genuinely cannot be reproduced - the writer
    /// throws a clear <see cref="NotSupportedException"/> for those rather than guessing/corrupting
    /// data. Every section BmdParser can only read when empty (its Read* always throws
    /// NotImplementedException otherwise) is written back as "version + count 0" and guarded the
    /// same way. <see cref="BmdParser.Parse(byte[])"/> re-reads the output as a self-check before
    /// returning, so a bug here fails loudly instead of silently corrupting a file on disk.
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
                    // "some_length" is read by BmdParser but never stored and never used to gate any
                    // subsequent read, so it's always safe to write 0 regardless of the original value.
                    writer.Write(GetVersion(bmdFile, "LegacyPreV22"));
                    writer.Write((uint)0);
                }

                writer.Write(GetVersion(bmdFile, "BattlefieldBuilding"));
                WriteCollection(writer, bmdFile.BattlefieldBuildings, WriteBattlefieldBuilding);

                WriteEmptyOnlyCollection(writer, bmdFile, "BattlefieldBuildingFar", bmdFile.BattlefieldBuildingFars, hasVersion: true);

                writer.Write(GetVersion(bmdFile, "CaptureLocation"));
                WriteCollection(writer, bmdFile.CaptureLocations, WriteCaptureLocation);

                WriteEmptyOnlyCollection(writer, bmdFile, "EFLine", bmdFile.EFLines, hasVersion: false);

                writer.Write((uint)bmdFile.GoOutlines.Count);
                foreach (var outline in bmdFile.GoOutlines)
                    WriteOutlinePoints(writer, outline.VertexList);

                writer.Write((uint)bmdFile.NonTerrainOutlines.Count);
                foreach (var outline in bmdFile.NonTerrainOutlines)
                    WriteOutlinePoints(writer, outline.VertexList);

                writer.Write(GetVersion(bmdFile, "ZonesTemplate"));
                WriteCollection(writer, bmdFile.ZonesTemplates, WriteZonesTemplate);

                writer.Write(GetVersion(bmdFile, "BmdInfo"));
                WriteCollection(writer, bmdFile.BmdInfos, WriteBmdInfo);

                WriteEmptyOnlyCollection(writer, bmdFile, "BmdOutline", bmdFile.BmdOutlines, hasVersion: true);
                WriteEmptyOnlyCollection(writer, bmdFile, "TerrainOutline", bmdFile.TerrainOutlines, hasVersion: false);
                WriteEmptyOnlyCollection(writer, bmdFile, "LiteBuildingOutline", bmdFile.LiteBuildingOutlines, hasVersion: false);

                if (v > 4)
                    WriteEmptyOnlyCollection(writer, bmdFile, "CameraZone", bmdFile.CameraZones, hasVersion: true);

                if (v > 7)
                {
                    WriteEmptyOnlyCollection(writer, bmdFile, "CivilianDeployment", bmdFile.CivilianDeployments, hasVersion: false);
                    WriteEmptyOnlyCollection(writer, bmdFile, "CivilianShelter", bmdFile.CivilianShelters, hasVersion: false);
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
                    WriteEmptyOnlyCollection(writer, bmdFile, "TerrainStencilBlendTriangle", bmdFile.TerrainStencilBlendTriangles, hasVersion: true);

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

                    WriteEmptyOnlyCollection(writer, bmdFile, "BmdCachedArea", bmdFile.BmdCachedAreas, hasVersion: true);
                }

                if (v > 23)
                    WriteEmptyOnlyCollection(writer, bmdFile, "ToggleableBuildingSlot", bmdFile.ToggleableBuildingSlots, hasVersion: true);

                if (v > 24)
                    WriteEmptyOnlyCollection(writer, bmdFile, "TerraindDecal", bmdFile.TerraindDecals, hasVersion: true);

                if (v > 25)
                {
                    WriteEmptyOnlyCollection(writer, bmdFile, "TreeListReference", bmdFile.TreeListReferences, hasVersion: true);
                    WriteEmptyOnlyCollection(writer, bmdFile, "GrassListReference", bmdFile.GrassListReferences, hasVersion: true);
                }

                if (v > 26)
                    WriteEmptyOnlyCollection(writer, bmdFile, "WaterOutline", bmdFile.WaterOutlines, hasVersion: false);
            }

            var bytes = stream.ToArray();
            SelfCheck(bmdFile, bytes);
            return bytes;
        }

        // ---------------------------------------------------------------------
        // Self-check
        // ---------------------------------------------------------------------

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
            CheckCount(nameof(BmdFile.CaptureLocations), source.CaptureLocations.Count, reread.CaptureLocations.Count);
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
            CheckCount("AiHints.PolyLines", source.AiHints.PolyLines.Count, reread.AiHints.PolyLines.Count);
            CheckCount("AiHints.PolyLinesList", source.AiHints.PolyLinesList.Count, reread.AiHints.PolyLinesList.Count);
        }

        // ---------------------------------------------------------------------
        // Section-level helpers
        // ---------------------------------------------------------------------

        private static ushort GetVersion(BmdFile bmdFile, string key) =>
            bmdFile.SectionVersions.TryGetValue(key, out var version) ? version : (ushort)0;

        private static void WriteCollection<T>(BinaryWriter writer, List<T> items, Action<BinaryWriter, T> writeItem)
        {
            writer.Write((uint)items.Count);
            foreach (var item in items)
                writeItem(writer, item);
        }

        private static void WriteEmptyOnlyCollection<T>(BinaryWriter writer, BmdFile bmdFile, string sectionName, List<T> items, bool hasVersion)
        {
            if (items.Count > 0)
                throw new NotSupportedException(
                    $"BmdWriter: '{sectionName}' has {items.Count} item(s), but BmdParser can't read even one " +
                    $"(its reader always throws NotImplementedException for this type) - so this section can only " +
                    "be safely written when empty. Remove them before saving.");

            if (hasVersion)
                writer.Write(GetVersion(bmdFile, sectionName));
            writer.Write((uint)0);
        }

        private static void WriteOutlinePoints(BinaryWriter writer, List<RmvVector2> points)
        {
            writer.Write((uint)points.Count);
            foreach (var p in points)
            {
                writer.Write(p.X);
                writer.Write(p.Y);
            }
        }

        // ---------------------------------------------------------------------
        // Item writers (mirror BmdParser's Read* methods one-for-one)
        // ---------------------------------------------------------------------

        private static void WriteBattlefieldBuilding(BinaryWriter writer, BattlefieldBuilding building)
        {
            writer.Write(building.Version);
            WriteString(writer, building.BuildingId);

            if (building.Version > 8)
                writer.Write(building.ParentId);
            else if (building.Version > 6)
                writer.Write((short)building.ParentId);

            if (building.Version > 4)
                WriteString(writer, building.BuildingKey);

            WriteString(writer, building.PositionType);

            if (building.Version < 6)
            {
                // Inverse of the old raw position/rotation/scale composition
                // (scaleMatrix * rotationMatrix * translationMatrix) - Matrix.Decompose assumes
                // exactly this row-vector TRS order, so it exactly undoes it.
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

            if (building.Version < 4)
            {
                writer.Write(building.StartingDamageUnary);
                writer.Write(Bool(building.OnFire));
                writer.Write(Bool(building.StartDisabled));
                writer.Write(Bool(building.WeakPoint));
                writer.Write(Bool(building.AiBreachable));
                writer.Write(Bool(building.Indestructible));
                writer.Write(Bool(building.Dockable));
                writer.Write(Bool(building.Toggleable));
                writer.Write(Bool(building.Lite));
            }
            else
            {
                writer.Write(building.PropertiesVersion);
                WriteString(writer, building.PropertiesBuildingId);
                writer.Write(building.StartingDamageUnary);
                if (building.PropertiesVersion > 1)
                {
                    writer.Write(Bool(building.OnFire));
                    writer.Write(Bool(building.StartDisabled));
                    writer.Write(Bool(building.WeakPoint));
                    writer.Write(Bool(building.AiBreachable));
                    writer.Write(Bool(building.Indestructible));
                    writer.Write(Bool(building.Dockable));
                    writer.Write(Bool(building.Toggleable));
                    writer.Write(Bool(building.Lite));
                }
                if (building.PropertiesVersion > 2)
                    writer.Write(Bool(building.CastShadows));
                if (building.PropertiesVersion > 3)
                    writer.Write(Bool(building.KeyBuilding));
                if (building.PropertiesVersion > 5)
                {
                    writer.Write(Bool(building.KeyBuildingUseFort));
                    writer.Write(Bool(building.IsPropInOutfield));
                }
                if (building.PropertiesVersion > 8)
                {
                    writer.Write(Bool(building.SettlementLevelConfigurable));
                    writer.Write(Bool(building.HideTooltip));
                    writer.Write(Bool(building.IncludeInFog));
                }
            }

            if (building.Version > 7)
                WriteString(writer, building.HeightMode);
            if (building.Version > 8)
                writer.Write(building.Uid);
        }

        private static void WriteCaptureLocation(BinaryWriter writer, CaptureLocation location)
        {
            if (location.Version > 2)
                throw new NotSupportedException(
                    "CaptureLocation version > 2 has a 4-byte field BmdParser reads and then overwrites/discards " +
                    "before storing (see the 'redo this' comments in ReadCaptureLocation) - cannot be reproduced.");

            writer.Write(location.Version);
            writer.Write(location.Zero);
            writer.Write(location.Something1);
            writer.Write(location.Something2);
            writer.Write(location.Something3);
            writer.Write(location.Something4);
            writer.Write(location.Something5);
            WriteString(writer, location.Str);
            writer.Write((uint)(location.Coords.Length / 2));
            foreach (var f in location.Coords)
                writer.Write(f);
            WriteString(writer, location.Str3);
            writer.Write(location.Something6);
            writer.Write(location.Something7);
            WriteFixedBytes(writer, location.Bools, 4);
        }

        private static void WriteZonesTemplate(BinaryWriter writer, ZonesTemplate template)
        {
            writer.Write((uint)template.Outline.Count);
            foreach (var p in template.Outline)
            {
                writer.Write(p.X);
                writer.Write(p.Y);
            }

            WriteString(writer, template.ZoneName);
            WriteString(writer, template.EntityFormationTemplateName);

            if (template.LinesLength != 0)
                throw new NotSupportedException("ZonesTemplate has a non-zero LinesLength - that data's structure is unknown to BmdParser (skipped, not stored), so it can't be written back.");
            writer.Write((uint)0);

            WriteRowMajorMatrix(writer, template.Transform, is4x4: true);
        }

        private static void WriteBmdInfo(BinaryWriter writer, BmdInfo bmd)
        {
            if (bmd.PropertyOverrides == 1)
                throw new NotSupportedException(
                    "BmdInfo.PropertyOverrides == 1 carries data BmdParser reads and discards (never stored on " +
                    "BmdInfo) - cannot be written back. BmdParser's own comment notes this is ~0.01% of real files.");
            if (bmd.Version is 4 or 5 or 6 or 7)
                throw new NotSupportedException($"BmdInfo version {bmd.Version} has filler bytes BmdParser reads and discards (never stored) - cannot be written back.");

            writer.Write(bmd.Version);
            WriteString(writer, bmd.BmdString);
            WriteRowMajorMatrix(writer, bmd.Transform, is4x4: true);
            writer.Write(bmd.PropertyOverrides);

            if (bmd.Version > 7)
            {
                WriteCultureMask(writer, bmd.CultureMask);
                WriteString(writer, bmd.RegionString);
            }
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
                // Preserve the original shared string table's order/entries for byte-identical
                // round-trips on unedited files; append any Rmv2Path a PropInfo now references
                // that wasn't already in it (e.g. a path changed via editing).
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
            if (prop.PropInfoVersion < 4)
                throw new NotSupportedException(
                    $"PropInfo version {prop.PropInfoVersion} predates the fields BmdParser stores (several bytes " +
                    "are read and discarded, never kept on PropInfo) - cannot be written back.");

            writer.Write(prop.PropInfoVersion);
            if (prop.PropInfoVersion <= 12)
            {
                WriteString(writer, prop.Rmv2Path);
            }
            else
            {
                // Real prop tables can contain duplicate paths - prefer the prop's own original
                // index (when it still points at the right string) over a fresh string lookup, so
                // an unedited save doesn't silently repoint it at a different, merely
                // string-identical, table slot.
                var index = prop.PropIndex is { } original && original < propsList.Count && propsList[original] == prop.Rmv2Path
                    ? original
                    : propIndexLookup[prop.Rmv2Path];
                writer.Write((uint)index);
            }

            WriteRowMajorMatrix(writer, prop.Transform, is4x4: false);

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
            {
                writer.Write(Bool(prop.VisibleInShroud));
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
            // else (<=10): BmdParser reads nothing for the culture mask at this version - write nothing.

            if (prop.PropInfoVersion > 11 || prop.PropInfoVersion == 9)
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

            if (prop.PropInfoVersion == 21)
                writer.Write(Bool(prop.SomeWeirdThing));
            if (prop.PropInfoVersion == 22)
            {
                writer.Write(Bool(prop.SomeWeirdThing));
                writer.Write(Bool(prop.SomeWeirdThing2));
            }

            if (prop.PropInfoVersion > 23)
                writer.Write(Bool(prop.UseDynamicShadows));
            if (prop.PropInfoVersion > 24)
                writer.Write(Bool(prop.UsesTerrainVertexOffset));
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
                // BmdParser names these locals "one"/"zero" - always these literal values.
                writer.Write((ushort)1);
                writer.Write((uint)0);
            }
            else
            {
                WriteBmdComponentFlags(writer, vfx.Flags);
            }

            if (vfx.VfxInfoVersion > 2)
                WriteString(writer, vfx.HeightMode);
            if (vfx.VfxInfoVersion > 3)
            {
                if (vfx.VfxInfoVersion > 5)
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
                writer.Write(Bool(vfx.VisibleInShroudOnly));
        }

        private static void WriteAiHints(BinaryWriter writer, BmdFile bmdFile)
        {
            var aiHints = bmdFile.AiHints;

            writer.Write(GetVersion(bmdFile, "AiHints.Separators"));
            writer.Write((uint)0); // BmdParser throws if Separators is ever non-empty

            writer.Write(GetVersion(bmdFile, "AiHints.DirectedPoints"));
            writer.Write((uint)0); // BmdParser throws if DirectedPoints is ever non-empty

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
                writer.Write(polyLineList.District);
                writer.Write((uint)polyLineList.PolygonList.Count);
                foreach (var polygon in polyLineList.PolygonList)
                {
                    writer.Write((uint)polygon.Points.Count);
                    foreach (var p in polygon.Points)
                    {
                        writer.Write(p.X);
                        writer.Write(p.Y);
                    }
                }
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
                writer.Write(probe.SomeZero);
            }

            writer.Write(Bool(probe.Primary));
            WriteString(writer, probe.HeightMode);
        }

        private static void WriteTerrainHoleInfo(BinaryWriter writer, TerrainHoleTriangleInfo hole)
        {
            writer.Write(hole.TerrainHoleVersion);
            WriteVector3(writer, hole.FirstVert);
            WriteVector3(writer, hole.SecondVert);
            WriteVector3(writer, hole.ThirdVert);
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
                writer.Write(Bool(area.Flag1));
                writer.Write(Bool(area.Flag2));
                writer.Write(Bool(area.Flag3));
                writer.Write(Bool(area.Flag4));
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
                WriteFixedBytes(writer, mesh.Booleans, 4);
                writer.Write(Bool(mesh.VisibleInShroud));
                WriteFixedBytes(writer, mesh.MoreBooleans, 1);
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
            writer.Write(Bool(light.Volumetric));
            WriteString(writer, light.HeightMode);

            if (light.Version > 4)
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

            writer.Write((uint)sound.RiverNodeList.Length);
            foreach (var node in sound.RiverNodeList)
            {
                writer.Write(node.Version);
                WriteVector3(writer, node.Position);
                writer.Write(node.Something1);
                writer.Write(node.Something2);
            }

            writer.Write(sound.ClampToSurface);
            WriteString(writer, sound.HeightMode);

            if (sound.Version > 9)
                writer.Write(sound.CampaignTypeMask);
            else
                writer.Write((uint)sound.CampaignTypeMask);

            if (sound.Version > 5)
                WriteCultureMask(writer, sound.CultureMask);
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
            WriteString(writer, csc.HeightMode);

            if (csc.Version > 2)
            {
                writer.Write(csc.PdlcMask);
                writer.Write(Bool(csc.Autoplay));
                writer.Write(Bool(csc.VisibleInShroud));
                writer.Write(Bool(csc.NoCulling));
            }
            if (csc.Version > 7)
            {
                WriteString(writer, csc.ScriptId);
                WriteString(writer, csc.ParentScriptId);
            }
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

        // ---------------------------------------------------------------------
        // Primitive helpers (mirror BmdParser's Read helpers one-for-one)
        // ---------------------------------------------------------------------

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

        private static void WriteCultureMask(BinaryWriter writer, CultureMask mask)
        {
            if (mask.RawBytes is { Length: 8 } raw)
            {
                writer.Write(raw);
                return;
            }

            var bytes = new byte[8];

            if (mask.CultMaskBase) bytes[0] |= 0x01;
            if (mask.CultMaskBst) bytes[0] |= 0x02;
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

            writer.Write(bytes);
        }
    }
}
