using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Bmd;
using Shared.GameFormats.RigidModel.Transforms;
using GameWorld.Core.SceneNodes;
using Serilog;
using Editors.BmdEditor.ViewModels;

namespace Editors.BmdEditor.Services
{
    /// <summary>
    /// Builds the 3D view of a BMD: its own elements, plus the content of each BMD it references.
    ///
    /// The scene is flat: groups keep an identity ModelMatrix, and each drawn node carries its full
    /// world matrix (its own placement, times the reference's placement for referenced content).
    /// Two reasons. Picking tests a mesh against its own ModelMatrix only, ignoring its parents, so
    /// a prop placed through a parent group would be picked as if it stood at the origin. And the
    /// scene manager multiplies parent matrices in the wrong order for nested placements
    /// (parent * child), which only gives the right result while at most one level is non-identity.
    /// </summary>
    public class BmdSceneCreator(GameWorld.Core.Components.SceneManager sceneManager, BmdPropMeshCache propMeshCache)
    {
        private readonly ILogger _logger = Log.ForContext<BmdSceneCreator>();

        public readonly Dictionary<BmdElementViewModel, SceneNode> ComponentNodes = new();
        private GroupNode? _bmdRootNode;
        private SceneNode? _currentHighlightedNode;

        /// <summary>
        /// Replaces the scene with one built from <paramref name="bmdFile"/> and its view models
        /// (<paramref name="allElements"/>, with each referenced BMD's elements under its
        /// <see cref="BmdInfoViewModel"/>). With <paramref name="reuseLoadedModels"/>, the new
        /// scene is built before the old one is removed, so prop models both use are not reloaded;
        /// without it, everything is read from the packs again.
        /// </summary>
        public void BuildScene(BmdFile bmdFile, IReadOnlyList<BmdElementViewModel> allElements, bool reuseLoadedModels)
        {
            var oldRoot = _bmdRootNode;
            if (!reuseLoadedModels)
            {
                RemoveFromScene(oldRoot);
                oldRoot = null;
                propMeshCache.RemoveUnused(includeFailures: true);
            }

            ComponentNodes.Clear();
            _currentHighlightedNode = null;

            var root = new GroupNode("BMD_Scene") { IsEditable = false };
            LoadContent(bmdFile, allElements, root, Matrix.Identity);
            _bmdRootNode = sceneManager.RootNode.AddObject(root);

            RemoveFromScene(oldRoot);
            propMeshCache.RemoveUnused(includeFailures: false);
        }

        // RemoveObject, unlike Children.Remove, tells every node it left the scene, which is what
        // frees their GPU buffers.
        private void RemoveFromScene(GroupNode? node)
        {
            if (node != null)
                sceneManager.RootNode.RemoveObject(node);
        }

        /// <summary>Adds the "Props" and "Other" groups for one BMD file under <paramref name="parent"/>,
        /// placing everything at <paramref name="world"/>.</summary>
        private void LoadContent(BmdFile bmdFile, IReadOnlyList<BmdElementViewModel> elements, GroupNode parent, Matrix world)
        {
            var propsGroup = parent.AddObject(new GroupNode("Props") { IsEditable = false });
            var otherGroup = parent.AddObject(new GroupNode("Other") { IsEditable = false });

            LoadComponents(bmdFile.PropInfos, elements.OfType<PropInfoViewModel>().ToList(), propsGroup, "Prop", (c, _, g, i) => CreatePropNode(c, g, i, world));

            var componentLoaders = new (string Name, int Count, Action<GroupNode> Loader)[]
            {
                ("Point_Lights", bmdFile.PointLights.Count, g => LoadComponents(bmdFile.PointLights, elements.OfType<PointLightInfoViewModel>().ToList(), g, "PointLight", (c, _, gg, i) => CreatePointLightNode(c, gg, i, world))),
                ("Spot_Lights", bmdFile.SpotLights.Count, g => LoadComponents(bmdFile.SpotLights, elements.OfType<SpotLightInfoViewModel>().ToList(), g, "SpotLight", (c, _, gg, i) => CreateSpotLightNode(c, gg, i, world))),
                ("Sounds", bmdFile.Sounds.Count, g => LoadComponents(bmdFile.Sounds, elements.OfType<SoundInfoViewModel>().ToList(), g, "Sound", (c, _, gg, i) => CreateSoundNode(c, gg, i, world))),
                ("VFX", bmdFile.VfxInfos.Count, g => LoadComponents(bmdFile.VfxInfos, elements.OfType<VfxInfoViewModel>().ToList(), g, "VFX", (c, _, gg, i) => CreateVfxNode(c, gg, i, world))),
                ("CSC", bmdFile.CscInfos.Count, g => LoadComponents(bmdFile.CscInfos, elements.OfType<CscInfoViewModel>().ToList(), g, "CSC", (c, _, gg, i) => CreateCscNode(c, gg, i, world))),
                ("Light_Probes", bmdFile.LightProbes.Count, g => LoadComponents(bmdFile.LightProbes, elements.OfType<LightProbeInfoViewModel>().ToList(), g, "LightProbe", (c, _, gg, i) => CreateLightProbeNode(c, gg, i, world))),
                ("Building_Projectile_Emitters", bmdFile.BuildingProjectileEmitters.Count, g => LoadComponents(bmdFile.BuildingProjectileEmitters, elements.OfType<BuildingProjectileEmitterViewModel>().ToList(), g, "BuildingProjectileEmitter", (c, _, gg, i) => CreateBuildingProjectileEmitterNode(c, gg, i, world))),
                ("Terrain_Holes", bmdFile.TerrainHoles.Count, g => LoadComponents(bmdFile.TerrainHoles, elements.OfType<TerrainHoleInfoViewModel>().ToList(), g, "TerrainHole", (c, _, gg, i) => CreateTerrainHoleNode(c, gg, i, world))),
                ("PolyMeshes", bmdFile.PolyMeshes.Count, g => LoadComponents(bmdFile.PolyMeshes, elements.OfType<PolyMeshInfoViewModel>().ToList(), g, "PolyMesh", (c, _, gg, i) => CreatePolyMeshNode(c, gg, i, world))),
                ("Go_Outlines", bmdFile.GoOutlines.Count, g => LoadComponents(bmdFile.GoOutlines, elements.OfType<GoOutlineViewModel>().ToList(), g, "GoOutline", (c, _, gg, i) => CreateGoOutlineNode(c, gg, i, world))),
                ("NonTerrain_Outlines", bmdFile.NonTerrainOutlines.Count, g => LoadComponents(bmdFile.NonTerrainOutlines, elements.OfType<NonTerrainOutlineViewModel>().ToList(), g, "NonTerrainOutline", (c, _, gg, i) => CreateNonTerrainOutlineNode(c, gg, i, world))),
                ("Battlefield_Buildings", bmdFile.BattlefieldBuildings.Count, g => LoadComponents(bmdFile.BattlefieldBuildings, elements.OfType<BattlefieldBuildingViewModel>().ToList(), g, "BattlefieldBuilding", (c, _, gg, i) => CreateBattlefieldBuildingNode(c, gg, i, world))),
                ("BMD_References", bmdFile.BmdInfos.Count, g => LoadComponents(bmdFile.BmdInfos, elements.OfType<BmdInfoViewModel>().ToList(), g, "BMD", (c, vm, gg, i) => CreateBmdInfoNode(c, vm, gg, i, world))),
                ("Deployments", bmdFile.Deployments.Count, g => LoadDeployments(bmdFile.Deployments, elements.OfType<DeploymentViewModel>().ToList(), g, world)),
            };

            foreach (var (name, count, loader) in componentLoaders)
            {
                if (count == 0)
                    continue;
                var group = otherGroup.AddObject(new GroupNode($"{name} ({count})") { IsEditable = false });
                loader(group);
            }
        }

        private void LoadComponents<T, TViewModel>(IList<T> components, IList<TViewModel> viewModels, GroupNode group, string prefix,
            Func<T, TViewModel, GroupNode, int, SceneNode> nodeCreator)
            where TViewModel : BmdElementViewModel
        {
            for (var i = 0; i < components.Count && i < viewModels.Count; i++)
            {
                try
                {
                    ComponentNodes[viewModels[i]] = nodeCreator(components[i], viewModels[i], group, i);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to create {prefix} component at index {i}: {ex.Message}");
                }
            }
        }

        private static T Placed<T>(T node, Matrix modelMatrix) where T : SceneNode
        {
            node.IsEditable = false;
            node.ModelMatrix = modelMatrix;
            return node;
        }

        private static GroupNode AddElementGroup(GroupNode parent, string name) =>
            parent.AddObject(new GroupNode(name) { IsEditable = false });

        private SceneNode CreatePropNode(PropInfo propInfo, GroupNode propsGroup, int index, Matrix world)
        {
            var propPath = propInfo.Rmv2Path;
            var propGroup = AddElementGroup(propsGroup, $"Prop_{index}_{Path.GetFileNameWithoutExtension(propPath)}");
            var placement = propInfo.Transform * world;

            var model = propMeshCache.Get(propPath);
            if (model.FailureReason == null)
            {
                foreach (var mesh in propMeshCache.CreateInstance(model))
                {
                    mesh.ModelMatrix = placement;
                    propGroup.AddObject(mesh);
                }
            }
            else
            {
                propGroup.AddObject(Placed(new PropPlaceholderNode("Prop_Placeholder") { FailedModelPath = propPath }, placement));
                _logger.Debug($"Created placeholder prop: {propPath} ({model.FailureReason})");
            }

            return propGroup;
        }

        private static SceneNode CreateVfxNode(VfxInfo vfxInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"VFX_{index}_{Path.GetFileNameWithoutExtension(vfxInfo.VfxString)}");
            node.AddObject(Placed(new VfxPlaceholderNode("VFX_Placeholder"), vfxInfo.Transform * world));
            return node;
        }

        private static SceneNode CreateCscNode(CscInfo cscInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"CSC_{index}_{Path.GetFileNameWithoutExtension(cscInfo.SceneFile)}");
            node.AddObject(Placed(new CscPlaceholderNode("CSC_Placeholder"), cscInfo.Transform * world));
            return node;
        }

        private static SceneNode CreateLightProbeNode(LightProbeInfo lightProbeInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"LightProbe_{index}");
            var placeholder = new LightProbePlaceholderNode("LightProbe_Placeholder")
            {
                OuterRadius = lightProbeInfo.OuterRadius,
                InnerRadius = lightProbeInfo.InnerRadius
            };
            node.AddObject(Placed(placeholder, Matrix.CreateTranslation(lightProbeInfo.Position.ToVector3()) * world));
            return node;
        }

        private static SceneNode CreateBuildingProjectileEmitterNode(BuildingProjectileEmitter emitterInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"BuildingProjectileEmitter_{index}");
            var position = new Vector3(emitterInfo.Location.X, emitterInfo.Location.Y, emitterInfo.Location.Z);
            node.AddObject(Placed(new BuildingProjectileEmitterPlaceholderNode("BuildingProjectileEmitter_Placeholder"), Matrix.CreateTranslation(position) * world));
            return node;
        }

        private static SceneNode CreateTerrainHoleNode(TerrainHoleTriangleInfo terrainHole, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"TerrainHole_{index}");
            var edges = new TerrainHoleEdgesNode("TerrainHole_Edges")
            {
                FirstVert = terrainHole.FirstVert,
                SecondVert = terrainHole.SecondVert,
                ThirdVert = terrainHole.ThirdVert
            };
            node.AddObject(Placed(edges, world));
            return node;
        }

        private static SceneNode CreatePolyMeshNode(PolyMeshInfo polyMesh, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"PolyMesh_{index}_{Path.GetFileNameWithoutExtension(polyMesh.MaterialString)}");
            var triangles = new PolyMeshTrianglesNode("PolyMesh_Triangles")
            {
                Vertices = polyMesh.VertexList,
                Triangles = polyMesh.TriangleList,
                MaterialString = polyMesh.MaterialString
            };

            // Version > 3 carries a real transform matrix; version <= 3 has none (vertices are
            // already baked into world space), matching BmdTerryProjectWriter's understanding.
            var local = polyMesh.PolyMeshVersion > 3 ? polyMesh.Transform : Matrix.Identity;
            node.AddObject(Placed(triangles, local * world));
            return node;
        }

        private static SceneNode CreateBattlefieldBuildingNode(BattlefieldBuilding buildingInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"BattlefieldBuilding_{index}_{buildingInfo.BuildingId}");
            node.AddObject(Placed(new BattlefieldBuildingPlaceholderNode("BattlefieldBuilding_Placeholder"), buildingInfo.Transform * world));
            return node;
        }

        private static SceneNode CreateGoOutlineNode(GoOutline outlineInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"GoOutline_{index}");
            node.AddObject(Placed(new GoOutlineNode("GoOutline_Placeholder") { VertexList = outlineInfo.VertexList }, world));
            return node;
        }

        private static SceneNode CreateNonTerrainOutlineNode(NonTerrainOutline outlineInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"NonTerrainOutline_{index}");
            node.AddObject(Placed(new NonTerrainOutlineNode("NonTerrainOutline_Placeholder") { VertexList = outlineInfo.VertexList }, world));
            return node;
        }

        private static SceneNode CreatePointLightNode(PointLightInfo pointLightInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"PointLight_{index}");
            var sphere = new PointLightSphereNode("PointLight_Sphere") { Radius = pointLightInfo.Radius };
            node.AddObject(Placed(sphere, Matrix.CreateTranslation(pointLightInfo.Position.ToVector3()) * world));
            return node;
        }

        private static SceneNode CreateSpotLightNode(SpotLightInfo spotLightInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"SpotLight_{index}");
            var cone = new SpotLightConeNode("SpotLight_Cone")
            {
                Length = spotLightInfo.Length,
                InnerAngle = spotLightInfo.InnerAngleRadians,
                OuterAngle = spotLightInfo.OuterAngleRadians,
                Position = spotLightInfo.Position,
                Quaternion = new Quaternion(spotLightInfo.QuartX, spotLightInfo.QuartY, spotLightInfo.QuartZ, spotLightInfo.QuartW)
            };
            node.AddObject(Placed(cone, world));
            return node;
        }

        private static SceneNode CreateSoundNode(SoundInfo soundInfo, GroupNode group, int index, Matrix world)
        {
            var node = AddElementGroup(group, $"Sound_{index}_{Path.GetFileNameWithoutExtension(soundInfo.SoundString)}");
            var placeholder = new SoundPlaceholderNode("Sound_Placeholder")
            {
                SoundType = soundInfo.TypeString,
                CoordList = soundInfo.CoordList
            };
            node.AddObject(Placed(placeholder, world));
            return node;
        }

        /// <summary>The reference's own marker, plus (for a reference the loader parsed) the
        /// referenced file's content placed at the reference's transform. The loader only parses
        /// references one level deep, so nested references show just their marker.</summary>
        private SceneNode CreateBmdInfoNode(BmdInfo bmdInfo, BmdInfoViewModel viewModel, GroupNode group, int index, Matrix world)
        {
            var name = Path.GetFileNameWithoutExtension(bmdInfo.BmdString);
            var node = AddElementGroup(group, $"BMD_{index}_{name}");
            var placement = bmdInfo.Transform * world;
            node.AddObject(Placed(new BmdInfoPlaceholderNode("BMD_Info_Placeholder") { ReferencedBmdPath = bmdInfo.BmdString }, placement));

            if (viewModel.ReferencedBmd != null)
            {
                var contentGroup = AddElementGroup(node, $"Referenced_Content_{name}");
                LoadContent(viewModel.ReferencedBmd, viewModel.ChildElements, contentGroup, placement);
            }

            return node;
        }

        private void LoadDeployments(List<Deployment> deployments, List<DeploymentViewModel> viewModels, GroupNode group, Matrix world)
        {
            for (var i = 0; i < deployments.Count && i < viewModels.Count; i++)
            {
                try
                {
                    var deployment = deployments[i];
                    var deploymentNode = AddElementGroup(group, $"Deployment_{i}_{deployment.Category}");
                    ComponentNodes[viewModels[i]] = deploymentNode;

                    var zoneViewModels = viewModels[i].Children.OfType<DeploymentZoneViewModel>().ToList();
                    for (var z = 0; z < deployment.DeploymentZones.Count && z < zoneViewModels.Count; z++)
                    {
                        var zone = deployment.DeploymentZones[z];
                        var zoneNode = AddElementGroup(deploymentNode, $"DeploymentZone_{z}");
                        ComponentNodes[zoneViewModels[z]] = zoneNode;

                        var regionViewModels = zoneViewModels[z].Children.OfType<DeploymentZoneRegionViewModel>().ToList();
                        for (var r = 0; r < zone.DeploymentZoneRegions.Count && r < regionViewModels.Count; r++)
                        {
                            var region = zone.DeploymentZoneRegions[r];
                            var regionNode = AddElementGroup(zoneNode, $"DeploymentZoneRegion_{region.Id}");
                            ComponentNodes[regionViewModels[r]] = regionNode;

                            var boundaryViewModels = regionViewModels[r].Children.OfType<BoundaryViewModel>().ToList();
                            for (var b = 0; b < region.Boundaries.Count && b < boundaryViewModels.Count; b++)
                            {
                                var boundary = region.Boundaries[b];
                                var boundaryNode = AddElementGroup(regionNode, $"Boundary_{b}_{boundary.BoundaryType}");
                                boundaryNode.AddObject(Placed(new BoundaryNode("Boundary_Placeholder") { PointList = boundary.PointList }, world));
                                ComponentNodes[boundaryViewModels[b]] = boundaryNode;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to create deployment component at index {i}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Pushes an edited view model's transform (from a textbox edit or a gizmo drag) into its
        /// scene node so the 3D view updates immediately, without rebuilding the scene. Each
        /// component's visual reads its position/orientation from a different place (some from
        /// their own ModelMatrix, some from bespoke fields the node's own Render reads directly -
        /// see the corresponding CreateXxxNode above), so this mirrors that per-type. Sound and
        /// world-space (PolyMeshVersion &lt;= 3) polymesh visuals need no push at all - they read
        /// straight from the same array the view model already mutated in place.
        /// Only the open file's own elements are edited, so the placements here are the file's own,
        /// without a reference's transform.
        /// </summary>
        public void RefreshVisual(BmdElementViewModel element)
        {
            if (!ComponentNodes.TryGetValue(element, out var node))
                return;

            switch (element)
            {
                case PropInfoViewModel propVm:
                    // Every child is either one mesh of the model, or the one placeholder.
                    foreach (var child in node.Children)
                        child.ModelMatrix = propVm.Prop.Transform;
                    break;

                case VfxInfoViewModel vfxVm:
                    SetChildModelMatrix(node, vfxVm.Vfx.Transform);
                    break;

                case CscInfoViewModel cscVm:
                    SetChildModelMatrix(node, cscVm.Csc.Transform);
                    break;

                case PointLightInfoViewModel lightVm:
                    SetChildModelMatrix(node, Matrix.CreateTranslation(lightVm.Light.Position.ToVector3()));
                    break;

                case LightProbeInfoViewModel probeVm:
                    SetChildModelMatrix(node, Matrix.CreateTranslation(probeVm.Probe.Position.ToVector3()));
                    break;

                case SpotLightInfoViewModel spotVm:
                {
                    var cone = FindChild<SpotLightConeNode>(node);
                    if (cone != null)
                    {
                        cone.Position = spotVm.Light.Position;
                        cone.Quaternion = new Quaternion(spotVm.Light.QuartX, spotVm.Light.QuartY, spotVm.Light.QuartZ, spotVm.Light.QuartW);
                    }
                    break;
                }

                case TerrainHoleInfoViewModel holeVm:
                {
                    var edges = FindChild<TerrainHoleEdgesNode>(node);
                    if (edges != null)
                    {
                        edges.FirstVert = holeVm.Hole.FirstVert;
                        edges.SecondVert = holeVm.Hole.SecondVert;
                        edges.ThirdVert = holeVm.Hole.ThirdVert;
                    }
                    break;
                }

                case PolyMeshInfoViewModel meshVm when meshVm.Mesh.PolyMeshVersion > 3:
                    SetChildModelMatrix(node, meshVm.Mesh.Transform);
                    break;
            }
        }

        private static void SetChildModelMatrix(SceneNode node, Matrix matrix)
        {
            var child = node.Children.FirstOrDefault();
            if (child != null)
                child.ModelMatrix = matrix;
        }

        private static T? FindChild<T>(SceneNode node) where T : class, ISceneNode =>
            node.Children.OfType<T>().FirstOrDefault();

        public void HighlightComponent(BmdElementViewModel component)
        {
            ClearHighlight();

            if (ComponentNodes.TryGetValue(component, out var node))
            {
                // TODO: Implement actual highlighting logic
                // This might involve changing the material, adding an outline, or changing the node's properties
                _currentHighlightedNode = node;
                _logger.Debug($"Highlighted component: {component.ElementType} - {component.DisplayName}");
            }
            else
            {
                _logger.Warning($"Component node not found: {component.ElementType} - {component.DisplayName}");
            }
        }

        public void ClearHighlight()
        {
            if (_currentHighlightedNode != null)
            {
                // TODO: Implement actual highlight clearing logic
                _logger.Debug($"Cleared highlight from: {_currentHighlightedNode.Name}");
                _currentHighlightedNode = null;
            }
        }
    }
}
