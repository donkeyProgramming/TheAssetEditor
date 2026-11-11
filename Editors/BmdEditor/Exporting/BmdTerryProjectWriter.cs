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
    /// Builds a Terry project (.terry) and its layer (.layer) from a parsed <see cref="BmdFile"/>.
    /// BmdInfo references are flattened into the same layer: a child's entities inherit the reference's
    /// culture mask, but its transform is not applied to them.
    /// </summary>
    public static class BmdTerryProjectWriter
    {
        public const string ProjectVersion = "27";
        public const string LayerVersion = "41";

        public readonly record struct TerryProject(string TerryXml, string LayerXml, string LayerEntityId);

        // Display order of the logical layers, which group the flattened entities by component type.
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
                Add(entitiesByCategory, "sounds", BuildSoundEntity(sound, inheritedCultureMask));

            foreach (var csc in bmd.CscInfos)
                Add(entitiesByCategory, "composite_scenes", BuildCscEntity(csc, inheritedCultureMask));

            foreach (var childRef in bmd.BmdInfos)
            {
                if (string.IsNullOrEmpty(childRef.BmdString) || !visited.Add(childRef.BmdString))
                    continue;

                var childBmd = resolveReferencedBmd(childRef.BmdString);
                if (childBmd == null)
                    continue;

                // In global_props.bin, where BMDs nest inside BMDs, a reference's campaign_type_mask
                // carries its child's culture as prefab_types bits. Terry-compiled prefabs leave it 0.
                CollectEntities(childBmd, TerryCultureMask.Format(childRef.CampaignTypeMask), entitiesByCategory, resolveReferencedBmd, visited);
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

        private static XElement BuildPropEntity(PropInfo prop, string cultureMask)
        {
            var t = TerryTransform.Decompose(prop.Transform);
            // Version >19 only: the stored bit is "visible without shroud", so shroud-only visibility is its inverse.
            var visibleInShroudOnly = prop.PropInfoVersion > 19 && !prop.VisibleWithoutShroud;

            var entity = NewEntity();
            if (prop.IsDecal)
            {
                var decal = new XElement("ECDecal",
                    new XAttribute("model_path", prop.Rmv2Path),
                    new XAttribute("parallax_scale", Fmt(prop.DecalParallaxScale)),
                    new XAttribute("tiling", Fmt(prop.DecalTiling)),
                    new XAttribute("normal_mode", "DNM_BLEND"),
                    new XAttribute("apply_to_terrain", Bool(prop.ApplyToTerrain)),
                    new XAttribute("apply_to_objects", Bool(prop.ApplyToPropsOrReceiveDecal)),
                    new XAttribute("render_above_snow", Bool(prop.RenderAboveSnow)));
                // Fields added after prop version 25; older props keep Terry's own defaults.
                if (prop.PropInfoVersion > 29)
                    decal.Add(new XAttribute("tiling_alpha_enabled", Bool(prop.DecalTilingAlpha)));
                if (prop.PropInfoVersion > 26)
                    decal.Add(new XAttribute("fade_start", Fmt(prop.DecalFadeStart)));
                if (prop.PropInfoVersion > 27)
                    decal.Add(
                        new XAttribute("mirror_x", Bool(prop.DecalMirrorX)),
                        new XAttribute("mirror_z", Bool(prop.DecalMirrorZ)),
                        new XAttribute("angle_threshold", Fmt(prop.DecalAngleThreshold)));
                if (prop.PropInfoVersion > 28)
                    decal.Add(new XAttribute("layer", prop.DecalLayer.ToString(CultureInfo.InvariantCulture)));
                entity.Add(decal);
            }
            else
            {
                entity.Add(new XElement("ECPropMesh"));
                entity.Add(new XElement("ECMesh", new XAttribute("model_path", prop.Rmv2Path), new XAttribute("opacity", "1")));
                var renderSettings = new XElement("ECMeshRenderSettings", new XAttribute("receive_decals", Bool(prop.ApplyToPropsOrReceiveDecal)));
                if (prop.PropInfoVersion > 11)
                    renderSettings.Add(new XAttribute("cast_shadow", Bool(prop.CastsShadow)));
                if (prop.PropInfoVersion > 18)
                    renderSettings.Add(new XAttribute("render_into_skydome_fog", Bool(prop.IncludeInFog)));
                if (prop.PropInfoVersion > 25)
                    renderSettings.Add(new XAttribute("blend_edges_with_terrain", Fmt(prop.BlendEdges)));
                entity.Add(renderSettings);
            }

            entity.Add(TacticalVisibility(prop.Flags));
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

        private static XElement BuildVfxEntity(VfxInfo vfx, string cultureMask)
        {
            var t = TerryTransform.Decompose(vfx.Transform);
            // Version >9 only: as with props, the stored bit is "visible without shroud" (Terry inverts it on compile).
            var visibleInShroudOnly = vfx.VfxInfoVersion > 9 && !vfx.VisibleWithoutShroud;

            return NewEntity(
                TacticalVisibility(vfx.Flags),
                // The scale is already in the transform, so the effect's own scale stays 1.
                new XElement("ECVFX",
                    new XAttribute("vfx", vfx.VfxString),
                    new XAttribute("autoplay", Bool(vfx.VfxInfoVersion is > 1 and < 5 || vfx.Autoplay)),
                    new XAttribute("scale", "1"),
                    new XAttribute("instance_name", vfx.InstanceName)),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", Bool(vfx.VisibleInShroud)),
                    new XAttribute("visible_in_shroud_only", Bool(visibleInShroudOnly)),
                    new XAttribute("no_culling", Bool(vfx.NoCulling)),
                    new XAttribute("culture_mask", cultureMask)),
                BuildTransformElement(t));
        }

        private static XElement BuildLightProbeEntity(LightProbeInfo probe)
        {
            var innerRadius = probe.Version > 2 ? probe.InnerRadius : probe.OuterRadius;

            // A cylinder probe's height isn't stored in the BMD, so Terry's default stands.
            return NewEntity(
                new XElement("ECLightProbe", new XAttribute("primary", Bool(probe.Primary))),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(probe.Position.ToVector3())),
                    new XAttribute("rotation", "0 0 0"),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")),
                new XElement(probe.IsCylinder ? "ECDoubleCylinder" : "ECDoubleSphere",
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
                TacticalVisibility(hole.Flags),
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
                TacticalVisibility(light.Flags),
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
                TacticalVisibility(mesh.Flags),
                PolyMeshCampaignProperties(mesh, visibleInShroud, inheritedCultureMask),
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
                TacticalVisibility(light.Flags),
                new XElement("ECCampaignProperties", new XAttribute("culture_mask", inheritedCultureMask)),
                new XElement("ECTransform",
                    new XAttribute("position", FormatVec(light.Position.ToVector3())),
                    new XAttribute("rotation", FormatVec(eulerDegrees)),
                    new XAttribute("scale", "1 1 1"),
                    new XAttribute("pivot", "0 0 0")));
        }

        private static XElement BuildSoundEntity(SoundInfo sound, string cultureMask)
        {
            var origin = sound.CoordList.Length > 0 ? sound.CoordList[0].ToVector3() : Vector3.Zero;

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
                    new XAttribute("script_id", csc.ScriptId),
                    new XAttribute("autoplay", Bool(csc.Autoplay)),
                    new XAttribute("hold_first_frame", Bool(csc.HoldFirst)),
                    new XAttribute("hold_last_frame", Bool(csc.HoldLast))),
                new XElement("ECVisibilitySettingsCampaign",
                    new XAttribute("visible_in_tactical_view", Bool(csc.VisibleInTacticalView)),
                    new XAttribute("visible_in_tactical_view_only", Bool(csc.VisibleInTacticalViewOnly))),
                BuildTransformElement(t),
                new XElement("ECCampaignProperties",
                    new XAttribute("visible_in_shroud", Bool(csc.VisibleInShroud)),
                    new XAttribute("visible_in_shroud_only", Bool(visibleInShroudOnly)),
                    new XAttribute("no_culling", Bool(csc.NoCulling)),
                    new XAttribute("culture_mask", inheritedCultureMask)));
        }

        private static XElement TacticalVisibility(BmdComponentFlags flags) =>
            new XElement("ECVisibilitySettingsCampaign",
                new XAttribute("visible_in_tactical_view", Bool(flags.VisibleInTactical)),
                new XAttribute("visible_in_tactical_view_only", Bool(flags.OnlyVisibleInTactical)));

        private static XElement PolyMeshCampaignProperties(PolyMeshInfo mesh, bool visibleInShroud, string inheritedCultureMask)
        {
            var properties = new XElement("ECCampaignProperties");
            // Version >3 only: the region flags and "visible without shroud" (inverted, as for props) came with the transform.
            if (mesh.PolyMeshVersion > 3)
                properties.Add(
                    new XAttribute("visible_inside_snow_region", Bool(mesh.VisibleInsideSnowRegion)),
                    new XAttribute("visible_outside_snow_region", Bool(mesh.VisibleOutsideSnowRegion)),
                    new XAttribute("visible_inside_destruction_region", Bool(mesh.VisibleInsideDestructionRegion)),
                    new XAttribute("visible_outside_destruction_region", Bool(mesh.VisibleOutsideDestructionRegion)));
            properties.Add(new XAttribute("visible_in_shroud", Bool(visibleInShroud)));
            if (mesh.PolyMeshVersion > 3)
                properties.Add(new XAttribute("visible_in_shroud_only", Bool(!mesh.VisibleWithoutShroud)));
            properties.Add(
                new XAttribute("no_culling", "true"),
                new XAttribute("culture_mask", inheritedCultureMask));
            return properties;
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
