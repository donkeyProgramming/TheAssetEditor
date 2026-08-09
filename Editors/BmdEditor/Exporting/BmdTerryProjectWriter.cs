using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Bmd;

namespace Editors.BmdEditor.Exporting
{
    /// <summary>
    /// Builds a Terry project (.terry) and its accompanying layer (.layer) from a parsed
    /// <see cref="BmdFile"/>, in the XML shape read by the Total War editor "Terry". BmdInfo
    /// references are recursively flattened into the same layer (their own Transform is not
    /// composed into descendants - only their culture mask is inherited - matching the reference
    /// Python export scripts this was ported from), so the result contains everything reachable
    /// from the root .bmd.
    /// </summary>
    public static class BmdTerryProjectWriter
    {
        public const string ProjectVersion = "27";
        public const string LayerVersion = "41";

        public readonly record struct TerryProject(string TerryXml, string LayerXml, string LayerEntityId);

        // Fixed display order for the logical layers grouping the flattened entities by component
        // type (matches the real editor's own "group by kind" habit, e.g. pyre_rock.*.layer, though
        // that sample groups by spatial batch rather than type - this groups by type instead since
        // that's what was asked for).
        private static readonly (string Key, string DisplayName)[] LayerCategories =
        {
            ("props", "Props"),
            ("decals", "Decals"),
            ("vfx", "VFX"),
            ("light_probes", "Light Probes"),
            ("terrain_holes", "Terrain Holes"),
            ("point_lights", "Point Lights"),
            ("poly_meshes", "Polygon Meshes"),
            ("spot_lights", "Spot Lights"),
            ("sounds", "Sounds"),
            ("composite_scenes", "Composite Scenes"),
        };

        public static TerryProject Build(BmdFile rootBmd, Func<string, BmdFile?> resolveReferencedBmd)
        {
            var entitiesByCategory = new Dictionary<string, List<XElement>>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectEntities(rootBmd, "", entitiesByCategory, resolveReferencedBmd, visited);

            var entitiesRoot = new XElement("entities");
            var logical = new XElement("Logical");
            foreach (var (key, displayName) in LayerCategories)
            {
                if (!entitiesByCategory.TryGetValue(key, out var members) || members.Count == 0)
                    continue;

                var layerId = TerryId.NewId();
                entitiesRoot.Add(NewLayerEntity(layerId, displayName));
                entitiesRoot.Add(members);

                var from = new XElement("from", new XAttribute("id", layerId));
                foreach (var member in members)
                    from.Add(new XElement("to", new XAttribute("id", (string)member.Attribute("id")!)));
                logical.Add(from);
            }

            var layerEntityId = TerryId.NewId();
            var projectId = TerryId.NewId();

            var layerDoc = new XDocument(
                new XElement("layer",
                    new XAttribute("version", LayerVersion),
                    entitiesRoot,
                    new XElement("associations",
                        logical,
                        new XElement("Transform"))));

            var terryDoc = new XDocument(
                new XElement("project",
                    new XAttribute("version", ProjectVersion),
                    new XAttribute("id", projectId),
                    new XElement("pc",
                        new XAttribute("type", "QTU::ProjectPrefab"),
                        new XElement("data", new XAttribute("database", "campaign"), new XAttribute("is_skybox", "0"))),
                    new XElement("pc",
                        new XAttribute("type", "QTU::Scene"),
                        new XElement("data",
                            new XAttribute("version", LayerVersion),
                            new XElement("entity",
                                new XAttribute("id", layerEntityId),
                                new XAttribute("name", "Default"),
                                new XElement("ECFileLayer", new XAttribute("export", "true"), new XAttribute("bmd_export_type", ""))))),
                    new XElement("pc", new XAttribute("type", "QTU::Terrain"))));

            return new TerryProject(ToXmlString(terryDoc), ToXmlString(layerDoc), layerEntityId);
        }

        private static void CollectEntities(BmdFile bmd, string inheritedCultureMask, Dictionary<string, List<XElement>> entitiesByCategory, Func<string, BmdFile?> resolveReferencedBmd, HashSet<string> visited)
        {
            foreach (var prop in bmd.PropInfos)
                Add(entitiesByCategory, prop.IsDecal ? "decals" : "props", BuildPropEntity(prop, inheritedCultureMask));

            foreach (var vfx in bmd.VfxInfos)
                Add(entitiesByCategory, "vfx", BuildVfxEntity(vfx, inheritedCultureMask));

            foreach (var probe in bmd.LightProbes)
                Add(entitiesByCategory, "light_probes", BuildLightProbeEntity(probe));

            foreach (var hole in bmd.TerrainHoles)
                Add(entitiesByCategory, "terrain_holes", BuildTerrainHoleEntity(hole, inheritedCultureMask));

            foreach (var light in bmd.PointLights)
                Add(entitiesByCategory, "point_lights", BuildPointLightEntity(light, inheritedCultureMask));

            foreach (var mesh in bmd.PolyMeshes)
                Add(entitiesByCategory, "poly_meshes", BuildPolyMeshEntity(mesh, inheritedCultureMask));

            foreach (var light in bmd.SpotLights)
                Add(entitiesByCategory, "spot_lights", BuildSpotLightEntity(light, inheritedCultureMask));

            foreach (var sound in bmd.Sounds)
                Add(entitiesByCategory, "sounds", BuildSoundEntity(sound));

            foreach (var csc in bmd.CscInfos)
                Add(entitiesByCategory, "composite_scenes", BuildCscEntity(csc, inheritedCultureMask));

            foreach (var childRef in bmd.BmdInfos)
            {
                if (string.IsNullOrEmpty(childRef.BmdString) || !visited.Add(childRef.BmdString))
                    continue;

                var childBmd = resolveReferencedBmd(childRef.BmdString);
                if (childBmd == null)
                    continue;

                CollectEntities(childBmd, TerryCultureMask.Format(childRef.CultureMask), entitiesByCategory, resolveReferencedBmd, visited);
            }
        }

        private static void Add(Dictionary<string, List<XElement>> entitiesByCategory, string category, XElement entity)
        {
            if (!entitiesByCategory.TryGetValue(category, out var list))
            {
                list = new List<XElement>();
                entitiesByCategory[category] = list;
            }
            list.Add(entity);
        }

        private static XElement NewLayerEntity(string id, string name) =>
            new XElement("entity",
                new XAttribute("id", id),
                new XAttribute("name", name),
                new XElement("ECLayer", new XAttribute("export", "true"), new XAttribute("bmd_export_type", "")),
                new XElement("ECToggleableBuildingsSlot",
                    new XAttribute("enabled", "false"),
                    new XAttribute("type", "TBST_TOWERS"),
                    new XAttribute("capture_location", ""),
                    new XAttribute("script_id", ""),
                    new XAttribute("map_barrier_record_key", ""),
                    new XAttribute("ground_melee_attack_allowed", "true")));

        private static XElement BuildPropEntity(PropInfo prop, string inheritedCultureMask)
        {
            var t = TerryTransform.Decompose(prop.Transform);
            var cultureMask = TerryCultureMask.PreferOwn(TerryCultureMask.Format(prop.CultureMask), inheritedCultureMask);
            // Version >19 only: the stored bit is "visible without shroud", so shroud-only visibility is its inverse.
            var visibleInShroudOnly = prop.PropInfoVersion > 19 && !prop.VisibleWithoutShroud;

            var entity = NewEntity();
            if (prop.IsDecal)
            {
                entity.Add(new XElement("ECDecal",
                    new XAttribute("model_path", prop.Rmv2Path),
                    new XAttribute("parallax_scale", "0"),
                    new XAttribute("tiling", "0"),
                    new XAttribute("normal_mode", "DNM_BLEND"),
                    new XAttribute("apply_to_terrain", Bool(prop.ApplyToTerrain)),
                    new XAttribute("apply_to_objects", Bool(prop.ApplyToPropsOrReceiveDecal)),
                    new XAttribute("render_above_snow", Bool(prop.RenderAboveSnow))));
            }
            else
            {
                entity.Add(new XElement("ECPropMesh"));
                entity.Add(new XElement("ECMesh", new XAttribute("model_path", prop.Rmv2Path), new XAttribute("opacity", "1")));
                entity.Add(new XElement("ECMeshRenderSettings", new XAttribute("receive_decals", Bool(prop.ApplyToPropsOrReceiveDecal))));
            }

            entity.Add(new XElement("ECVisibilitySettingsCampaign",
                new XAttribute("visible_in_tactical_view", Bool(prop.Flags.VisibleInTactical)),
                new XAttribute("visible_in_tactical_view_only", Bool(prop.Flags.OnlyVisibleInTactical))));
            entity.Add(new XElement("ECPropHeightPatch",
                new XAttribute("apply_height_patch", Bool(prop.ApplyHeightPatch)),
                new XAttribute("for_camera_height_map_only", "false")));
            entity.Add(new XElement("ECCampaignProperties",
                new XAttribute("visible_inside_snow_region", Bool(prop.VisibleInsideSnowRegion)),
                new XAttribute("visible_outside_snow_region", Bool(prop.VisibleOutsideSnowRegion)),
                new XAttribute("visible_inside_destruction_region", Bool(prop.VisibleInsideDestructionRegion)),
                new XAttribute("visible_outside_destruction_region", Bool(prop.VisibleOutsideDestructionRegion)),
                new XAttribute("visible_in_shroud", Bool(prop.VisibleInShroud)),
                new XAttribute("visible_in_shroud_only", Bool(visibleInShroudOnly)),
                new XAttribute("no_culling", Bool(prop.NoCulling)),
                new XAttribute("culture_mask", cultureMask)));
            entity.Add(BuildTransformElement(t));
            return entity;
        }

        private static XElement BuildVfxEntity(VfxInfo vfx, string inheritedCultureMask)
        {
            var t = TerryTransform.Decompose(vfx.Transform);
            var cultureMask = TerryCultureMask.PreferInherited(TerryCultureMask.Format(vfx.CultureMask), inheritedCultureMask);

            return NewEntity(
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", Bool(vfx.Flags.VisibleInTactical)),
                    new XAttribute("visible_in_tactical_view_only", Bool(vfx.Flags.OnlyVisibleInTactical))),
                // autoplay/scale are hardcoded here to match the reference export scripts, not vfx.Autoplay/the transform scale.
                new XElement("ECVFX",
                    new XAttribute("vfx", vfx.VfxString),
                    new XAttribute("autoplay", "true"),
                    new XAttribute("scale", "1"),
                    new XAttribute("instance_name", vfx.InstanceName)),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", Bool(vfx.VisibleInShroud)),
                    new XAttribute("visible_in_shroud_only", Bool(vfx.VisibleInShroudOnly)),
                    new XAttribute("culture_mask", cultureMask)),
                BuildTransformElement(t));
        }

        private static XElement BuildLightProbeEntity(LightProbeInfo probe)
        {
            var innerRadius = probe.Version > 2 ? probe.InnerRadius : probe.OuterRadius;

            return NewEntity(
                new XElement("ECLightProbe", new XAttribute("primary", Bool(probe.Primary))),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(probe.Position.ToVector3())),
                    new XAttribute("rotation", "0 0 0"),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")),
                new XElement("ECDoubleSphere",
                    new XAttribute("inner_radius", Fmt(innerRadius)),
                    new XAttribute("outer_radius", Fmt(probe.OuterRadius))));
        }

        private static XElement BuildTerrainHoleEntity(TerrainHoleTriangleInfo hole, string inheritedCultureMask)
        {
            var (position, eulerDegrees, localV2, localV3) = TerryTransform.FlattenTriangle(hole.FirstVert.ToVector3(), hole.SecondVert.ToVector3(), hole.ThirdVert.ToVector3());

            return NewEntity(
                new XElement("ECTerrainHole"),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(position)),
                    new XAttribute("rotation", FormatVec(eulerDegrees)),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", "false"),
                    new XAttribute("visible_in_tactical_view_only", "false")),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", "true"),
                    new XAttribute("no_culling", "true"),
                    new XAttribute("culture_mask", inheritedCultureMask)),
                new XElement("ECPolyline",
                    new XElement("polyline", new XAttribute("closed", "true"),
                        new XElement("point", new XAttribute("x", Fmt(0)), new XAttribute("y", Fmt(0))),
                        new XElement("point", new XAttribute("x", Fmt(localV2.X)), new XAttribute("y", Fmt(localV2.Z))),
                        new XElement("point", new XAttribute("x", Fmt(localV3.X)), new XAttribute("y", Fmt(localV3.Z))))));
        }

        private static XElement BuildPointLightEntity(PointLightInfo light, string inheritedCultureMask)
        {
            var animType = light.AnimationTypeEnum switch
            {
                1 => "LAT_RADIUS_SIN",
                2 => "LAT_RADIUS_SIN_SIN",
                _ => "LAT_NONE",
            };

            return NewEntity(
                new XElement("ECPointLight",
                    new XAttribute("colour", $"{(int)(light.Red * 255)} {(int)(light.Green * 255)} {(int)(light.Blue * 255)} 255"),
                    new XAttribute("colour_scale", Fmt(light.ColorScale)),
                    new XAttribute("radius", Fmt(light.Radius)),
                    new XAttribute("animation_type", animType),
                    new XAttribute("animation_speed_scale", $"{Fmt(light.AnimationSpeedScale1)} {Fmt(light.AnimationSpeedScale2)}"),
                    new XAttribute("colour_min", Fmt(light.ColorMin)),
                    new XAttribute("random_offset", Fmt(light.RandomOffset)),
                    new XAttribute("falloff_type", light.FalloffType),
                    new XAttribute("for_light_probes_only", Bool(light.LightProbeOnly))),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", "false"),
                    new XAttribute("visible_in_tactical_view_only", "false")),
                new XElement("ECCampaignProperties", new XAttribute("culture_mask", inheritedCultureMask)),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(light.Position.ToVector3())),
                    new XAttribute("rotation", "0 0 0"),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")));
        }

        private static XElement BuildPolyMeshEntity(PolyMeshInfo mesh, string inheritedCultureMask)
        {
            TerryTransform.Decomposed t;
            bool visibleInShroud;
            if (mesh.PolyMeshVersion > 3)
            {
                t = TerryTransform.Decompose(mesh.Transform);
                visibleInShroud = mesh.VisibleInShroud;
            }
            else
            {
                var y = mesh.VertexList.Length > 0 ? mesh.VertexList[0].Y : 0f;
                t = new TerryTransform.Decomposed(new Vector3(0, y, 0), Vector3.Zero, Vector3.One);
                visibleInShroud = false;
            }

            var polyline = new XElement("polyline", new XAttribute("closed", "true"));
            foreach (var v in mesh.VertexList)
                polyline.Add(new XElement("point", new XAttribute("x", Fmt(v.X)), new XAttribute("y", Fmt(v.Z))));

            return NewEntity(
                new XElement("ECPolygonMesh",
                    new XAttribute("material", mesh.MaterialString),
                    new XAttribute("affects_mesh_optimization", "false")),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", Bool(mesh.Flags.VisibleInTactical)),
                    new XAttribute("visible_in_tactical_view_only", Bool(mesh.Flags.OnlyVisibleInTactical))),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", Bool(visibleInShroud)),
                    new XAttribute("no_culling", "true"),
                    new XAttribute("culture_mask", inheritedCultureMask)),
                BuildTransformElement(t),
                new XElement("ECPolyline", polyline));
        }

        private static XElement BuildSpotLightEntity(SpotLightInfo light, string inheritedCultureMask)
        {
            var eulerDegrees = TerryTransform.QuaternionToEulerDegrees(light.QuartX, light.QuartY, light.QuartZ, light.QuartW);

            var maxIntensity = MathF.Max(light.IntensityRed, MathF.Max(light.IntensityGreen, light.IntensityBlue));
            int r = 0, g = 0, b = 0;
            if (maxIntensity > 0f)
            {
                r = (int)(light.IntensityRed / maxIntensity * 255);
                g = (int)(light.IntensityGreen / maxIntensity * 255);
                b = (int)(light.IntensityBlue / maxIntensity * 255);
            }

            return NewEntity(
                new XElement("ECSpotLight",
                    new XAttribute("colour", $"{r} {g} {b} 255"),
                    new XAttribute("intensity", Fmt(maxIntensity)),
                    new XAttribute("length", Fmt(light.Length)),
                    new XAttribute("inner_angle", Fmt(MathHelper.ToDegrees(light.InnerAngleRadians))),
                    new XAttribute("outer_angle", Fmt(MathHelper.ToDegrees(light.OuterAngleRadians))),
                    new XAttribute("falloff", Fmt(light.Falloff)),
                    new XAttribute("volumetric", Bool(light.Volumetric)),
                    new XAttribute("gobo", light.Gobo)),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", "false"),
                    new XAttribute("visible_in_tactical_view_only", "false")),
                new XElement("ECCampaignProperties", new XAttribute("culture_mask", inheritedCultureMask)),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(light.Position.ToVector3())),
                    new XAttribute("rotation", FormatVec(eulerDegrees)),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")));
        }

        private static XElement BuildSoundEntity(SoundInfo sound)
        {
            var origin = sound.CoordList.Length > 0 ? sound.CoordList[0].ToVector3() : Vector3.Zero;
            var cultureMask = TerryCultureMask.Format(sound.CultureMask);

            var entity = NewEntity(
                new XElement("ECSoundMarker", new XAttribute("key", sound.SoundString)),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(origin)),
                    new XAttribute("rotation", "0 0 0"),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")),
                new XElement("ECCampaignProperties", new XAttribute("culture_mask", cultureMask)));

            switch (sound.TypeString)
            {
                case "SST_LINE_LIST":
                {
                    var polyline3d = new XElement("polyline3d", new XAttribute("closed", "false"));
                    foreach (var c in sound.CoordList)
                    {
                        var v = c.ToVector3() - origin;
                        polyline3d.Add(new XElement("point", new XAttribute("x", Fmt(v.X)), new XAttribute("y", Fmt(v.Y)), new XAttribute("z", Fmt(v.Z))));
                    }
                    entity.Add(new XElement("ECPolyline3D", polyline3d));
                    break;
                }
                case "SST_MULTI_POINT":
                {
                    var pointCloud = new XElement("point_cloud");
                    foreach (var c in sound.CoordList)
                    {
                        var v = c.ToVector3() - origin;
                        pointCloud.Add(new XElement("point", new XAttribute("x", Fmt(v.X)), new XAttribute("y", Fmt(v.Y)), new XAttribute("z", Fmt(v.Z))));
                    }
                    entity.Add(new XElement("ECPointCloud", pointCloud));
                    break;
                }
                case "SST_SPHERE":
                    entity.Add(new XElement("ECSphere", new XAttribute("radius", Fmt(sound.OuterRadius))));
                    break;
            }

            return entity;
        }

        private static XElement BuildCscEntity(CscInfo csc, string inheritedCultureMask)
        {
            var t = TerryTransform.Decompose(csc.Transform);
            // Version >9 only: same "visible without shroud" bit-inversion as PropInfo above.
            var visibleInShroudOnly = csc.Version > 9 && !csc.VisibleWithoutShroud;

            return NewEntity(
                new XElement("ECCompositeScene",
                    new XAttribute("path", csc.SceneFile),
                    new XAttribute("script_id", ""),
                    new XAttribute("autoplay", "true")),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", "false"),
                    new XAttribute("visible_in_tactical_view_only", "false")),
                BuildTransformElement(t),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", Bool(csc.VisibleInShroud)),
                    new XAttribute("visible_in_shroud_only", Bool(visibleInShroudOnly)),
                    new XAttribute("no_culling", Bool(csc.NoCulling)),
                    new XAttribute("culture_mask", inheritedCultureMask)));
        }

        private static XElement NewEntity(params object[] components) =>
            new XElement("entity", new XAttribute("id", TerryId.NewId()), components);

        private static XElement BuildTransformElement(TerryTransform.Decomposed t) =>
            new XElement("ECTransform",
                new XAttribute("position", FormatVec(t.Position)),
                new XAttribute("rotation", FormatVec(t.EulerDegrees)),
                new XAttribute("scale", FormatVec(t.Scale)),
                new XAttribute("pivot", "0 0 0"));

        private static string Bool(bool b) => b ? "true" : "false";
        private static string Fmt(float f) => f.ToString(CultureInfo.InvariantCulture);
        private static string FormatVec(Vector3 v) => $"{Fmt(v.X)} {Fmt(v.Y)} {Fmt(v.Z)}";

        private static string ToXmlString(XDocument doc)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
            };
            using var stringWriter = new Utf8StringWriter();
            using (var writer = XmlWriter.Create(stringWriter, settings))
                doc.Save(writer);
            return stringWriter.ToString();
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
