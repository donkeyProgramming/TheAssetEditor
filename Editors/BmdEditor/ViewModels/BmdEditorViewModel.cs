using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.Bmd;
using GameWorld.Core.Services;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Serilog;
using Editors.BmdEditor.Services; // Added missing using directive

namespace Editors.BmdEditor.ViewModels
{
    public class BmdEditorViewModel : NotifyPropertyChangedImpl, IEditorInterface, IFileEditor, IDisposable
    {
        private readonly IPackFileService _packFileService;
        private readonly IEditorManager _editorCreator;
        private readonly IStandardDialogs _standardDialogs;
        private readonly GameWorld.Core.Services.ResourceLibrary _resourceLibrary;
        private readonly Shared.Core.Events.IEventHub _eventHub;
        private readonly GameWorld.Core.Services.IGraphicsResourceCreator _graphicsResourceCreator;
        private BmdFile? _bmdFile;

        public string DisplayName { get; set; } = "Not set";
        public PackFile CurrentFile { get; private set; } = null!;
        public string StatusText { get; set; } = "Ready";

        // BMD File Properties
        public BmdFile? BmdFile
        {
            get => _bmdFile;
            set => SetAndNotify(ref _bmdFile, value);
        }

        // Collections for different element types
        public ObservableCollection<BmdElementViewModel> AllElements { get; } = new();
        public ObservableCollection<BattlefieldBuildingViewModel> BattlefieldBuildings { get; } = new();
        public ObservableCollection<BattlefieldBuildingFarViewModel> BattlefieldBuildingFars { get; } = new();
        public ObservableCollection<CaptureLocationViewModel> CaptureLocations { get; } = new();
        public ObservableCollection<EFLineViewModel> EFLines { get; } = new();
        public ObservableCollection<GoOutlineViewModel> GoOutlines { get; } = new();
        public ObservableCollection<NonTerrainOutlineViewModel> NonTerrainOutlines { get; } = new();
        public ObservableCollection<BuildingProjectileEmitterViewModel> BuildingProjectileEmitters { get; } = new();
        public ObservableCollection<ZonesTemplateViewModel> ZonesTemplates { get; } = new();
        public ObservableCollection<BmdInfoViewModel> BmdInfos { get; } = new();
        public ObservableCollection<PropInfoViewModel> Props { get; } = new();
        public ObservableCollection<VfxInfoViewModel> VfxInfos { get; } = new();
        public ObservableCollection<PointLightInfoViewModel> PointLights { get; } = new();
        public ObservableCollection<SpotLightInfoViewModel> SpotLights { get; } = new();
        public ObservableCollection<SoundInfoViewModel> Sounds { get; } = new();
        public ObservableCollection<PolyMeshInfoViewModel> PolyMeshes { get; } = new();
        public ObservableCollection<LightProbeInfoViewModel> LightProbes { get; } = new();
        public ObservableCollection<TerrainHoleInfoViewModel> TerrainHoles { get; } = new();
        public ObservableCollection<PlayableAreaViewModel> PlayableAreas { get; } = new();
        public ObservableCollection<CscInfoViewModel> CscInfos { get; } = new();
        public ObservableCollection<DeploymentViewModel> Deployments { get; } = new();

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand NavigateToReferencedFileCommand { get; }

        // Selected component for details display
        private BmdElementViewModel? _selectedComponent;
        public BmdElementViewModel? SelectedComponent
        {
            get => _selectedComponent;
            set => SetAndNotify(ref _selectedComponent, value);
        }

        // Component details for display
        private string _componentDetails = "Select a component to view details";
        public string ComponentDetails
        {
            get => _componentDetails;
            set => SetAndNotify(ref _componentDetails, value);
        }

        private readonly BmdSceneCreator _bmdSceneCreator;
        private readonly SelectionManager _selectionManager;
        private readonly BmdElementLoader _bmdElementLoader;

        public IWpfGame Scene { get; set; }

        public BmdEditorViewModel(
            IPackFileService packFileService,
            IEditorManager editorCreator,
            IStandardDialogs standardDialogs,
            MeshBuilderService meshBuilderService, 
            CapabilityMaterialFactory materialFactory,
            GameWorld.Core.Services.ResourceLibrary resourceLibrary,
            Shared.Core.Events.IEventHub eventHub,
            GameWorld.Core.Services.IGraphicsResourceCreator graphicsResourceCreator,
            IWpfGame gameWorld,
            BmdSceneCreator bmdSceneCreator,
            SelectionManager selectionManager,
            IComponentInserter componentInserter,
            BmdElementLoader bmdElementLoader)
        {
            _packFileService = packFileService;
            _editorCreator = editorCreator;
            _standardDialogs = standardDialogs;
            _resourceLibrary = resourceLibrary;
            _eventHub = eventHub;
            _graphicsResourceCreator = graphicsResourceCreator;
            _bmdSceneCreator = bmdSceneCreator;
            _selectionManager = selectionManager;
            _bmdElementLoader = bmdElementLoader;
            
            Scene = gameWorld;

            // Ensure all game components are added to the editor
            componentInserter.Execute();

            RefreshCommand = new RelayCommand(Refresh);
            ExportCommand = new RelayCommand(Export);
            NavigateToReferencedFileCommand = new RelayCommand<string>(NavigateToReferencedFile);
            
            // Subscribe to selection changes from 3D view
            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);
            System.Diagnostics.Debug.WriteLine("BmdEditorViewModel: Subscribed to SelectionChangedEvent");
            
            if (_selectionManager == null)
            {
                System.Diagnostics.Debug.WriteLine("BmdEditorViewModel: WARNING - SelectionManager is null!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BmdEditorViewModel: SelectionManager is properly initialized");
            }
        }



        public void LoadFile(PackFile packFile)
        {
            CurrentFile = packFile;
            DisplayName = packFile.Name;

            try
            {
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Loading file: {packFile.Name}, Size: {packFile.DataSource.Size} bytes");
                
                var data = packFile.DataSource.ReadData();
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Read {data.Length} bytes from file");
                
                using var stream = new MemoryStream(data);
                var parser = new BmdParser(stream);
                
                BmdFile = parser.Parse();
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Parsing completed successfully");

                // Create 3D scene structure first
                _bmdSceneCreator.CreateSceneFromBmd(BmdFile!, packFile);
                System.Diagnostics.Debug.WriteLine($"BMD Editor - 3D scene structure created");

                // Update collections (this will load scene content)
                PopulateViewModels();
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Collections and scene content loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Failed to load BMD file {packFile.Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"BMD Editor - Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void PopulateViewModels()
        {
            if (BmdFile == null) return;

            // Clear existing collections
            AllElements.Clear();
            BattlefieldBuildings.Clear();
            BattlefieldBuildingFars.Clear();
            CaptureLocations.Clear();
            EFLines.Clear();
            GoOutlines.Clear();
            NonTerrainOutlines.Clear();
            BuildingProjectileEmitters.Clear();
            ZonesTemplates.Clear();
            BmdInfos.Clear();
            Props.Clear();
            VfxInfos.Clear();
            PointLights.Clear();
            SpotLights.Clear();
            Sounds.Clear();
            PolyMeshes.Clear();
            LightProbes.Clear();
            TerrainHoles.Clear();
            PlayableAreas.Clear();
            CscInfos.Clear();
            Deployments.Clear();

            // Use the BmdElementLoader to populate all collections
            _bmdElementLoader.LoadElements(BmdFile, 
                AllElements, BmdInfos, BattlefieldBuildings, BattlefieldBuildingFars,
                CaptureLocations, EFLines, GoOutlines, NonTerrainOutlines,
                BuildingProjectileEmitters, ZonesTemplates, Props, VfxInfos,
                PointLights, SpotLights, Sounds, PolyMeshes, LightProbes,
                TerrainHoles, PlayableAreas, CscInfos, Deployments,
                loadChildBmds: true);
        }

        private void Refresh()
        {
            if (CurrentFile != null)
            {
                LoadFile(CurrentFile);
            }
        }

        private void Export()
        {
            // TODO: Implement export functionality
            System.Diagnostics.Debug.WriteLine("Export BMD file - not yet implemented");
        }

        private void NavigateToReferencedFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            try
            {
                // Try to find the referenced file in the pack file system
                var referencedFile = _packFileService.FindFile(fileName);
                if (referencedFile != null)
                {
                    // Open the referenced file in the appropriate editor
                    var openCommand = new Shared.Core.Events.Global.OpenEditorCommand(_editorCreator, _packFileService);
                    openCommand.Execute(referencedFile);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Referenced file not found: {fileName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to navigate to referenced file {fileName}: {ex.Message}");
            }
        }

        public void SelectComponent(BmdElementViewModel component)
        {
            SelectedComponent = component;
            ComponentDetails = GenerateComponentDetails(component);
            
            System.Diagnostics.Debug.WriteLine($"SelectComponent called: {component.ElementType} - {component.DisplayName}");
            
            // Add visible debug info to component details
            ComponentDetails += $"\n\n[DEBUG] SelectComponent called: {component.ElementType} - {component.DisplayName}";
            
            // Select the component in the 3D scene using SelectionManager
            SelectComponentIn3DScene(component);
            
            System.Diagnostics.Debug.WriteLine($"SelectComponent completed: {component.ElementType} - {component.DisplayName}");
        }

        private void SelectComponentIn3DScene(BmdElementViewModel component)
        {
            System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: Looking for component {component.ElementType} - {component.DisplayName}");
            
            // Add visible debug info
            ComponentDetails += $"\n[DEBUG] Looking for component: {component.ElementType} - {component.DisplayName}";
            
            if (_bmdSceneCreator.ComponentNodes.TryGetValue(component, out var sceneNode))
            {
                System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: Found scene node '{sceneNode.Name}' for component {component.ElementType} - {component.DisplayName}");
                ComponentDetails += $"\n[DEBUG] Found scene node: {sceneNode.Name}";
                
                // Find the first selectable child node (Rmv2MeshNode implements ISelectable)
                var selectableNode = FindFirstSelectableNode(sceneNode);
                if (selectableNode != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: Found selectable node '{selectableNode.Name}' for component {component.ElementType} - {component.DisplayName}");
                    ComponentDetails += $"\n[DEBUG] Found selectable node: {selectableNode.Name}";
                    
                    // Clear current selection and select the new object
                    var objectSelection = _selectionManager.GetState<ObjectSelectionState>();
                    if (objectSelection != null)
                    {
                        objectSelection.Clear();
                        objectSelection.ModifySelectionSingleObject(selectableNode, false);
                        System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: Successfully selected 3D object: {component.ElementType} - {component.DisplayName}");
                        ComponentDetails += $"\n[DEBUG] Successfully selected 3D object!";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: ObjectSelectionState is null!");
                        ComponentDetails += $"\n[DEBUG] ERROR: ObjectSelectionState is null!";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: No selectable node found for component: {component.ElementType} - {component.DisplayName}");
                    ComponentDetails += $"\n[DEBUG] ERROR: No selectable node found!";
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SelectComponentIn3DScene: 3D object not found for component: {component.ElementType} - {component.DisplayName}");
                ComponentDetails += $"\n[DEBUG] ERROR: 3D object not found!";
            }
        }

        private ISelectable? FindFirstSelectableNode(ISceneNode sceneNode)
        {
            // Check if the current node is selectable
            if (sceneNode is ISelectable selectable)
                return selectable;
            
            // Recursively search children
            foreach (var child in sceneNode.Children)
            {
                var found = FindFirstSelectableNode(child);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        private string GenerateComponentDetails(BmdElementViewModel component)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"Type: {component.ElementType}");
            details.AppendLine($"Name: {component.DisplayName}");
            details.AppendLine($"Description: {component.Description}");
            details.AppendLine();

            // Add type-specific details
            switch (component)
            {
                case PropInfoViewModel prop:
                    details.AppendLine("Prop Details:");
                    details.AppendLine($"  Version: {prop.Prop.PropInfoVersion}");
                    details.AppendLine($"  File Path: {prop.PropFilePath}");
                    details.AppendLine($"  Is Decal: {prop.Prop.IsDecal}");
                    details.AppendLine($"  Animated: {prop.Prop.Animated}");
                    details.AppendLine($"  Casts Shadow: {prop.Prop.CastsShadow}");
                    details.AppendLine($"  RMV2 Path: {prop.Prop.Rmv2Path}");
                    details.AppendLine($"  Transform: {prop.Prop.Transform}");
                    break;

                case VfxInfoViewModel vfx:
                    details.AppendLine("VFX Details:");
                    details.AppendLine($"  Version: {vfx.Vfx.VfxInfoVersion}");
                    details.AppendLine($"  VFX String: {vfx.Vfx.VfxString}");
                    details.AppendLine($"  Flag Version: {vfx.Vfx.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {vfx.Vfx.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Water: {vfx.Vfx.ClampToWaterSurface}");
                    details.AppendLine($"  Visible In Tactical: {vfx.Vfx.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {vfx.Vfx.OnlyVisibleInTactical}");
                    details.AppendLine($"  Autoplay: {vfx.Vfx.Autoplay}");
                    details.AppendLine($"  Visible In Shroud: {vfx.Vfx.VisibleInShroud}");
                    details.AppendLine($"  Parent ID: {vfx.Vfx.ParentId}");
                    break;

                case PointLightInfoViewModel light:
                    details.AppendLine("Point Light Details:");
                    details.AppendLine($"  Version: {light.Light.PointLightInfoVersion}");
                    details.AppendLine($"  Position: ({light.Light.Position.X:F2}, {light.Light.Position.Y:F2}, {light.Light.Position.Z:F2})");
                    details.AppendLine($"  Radius: {light.Light.Radius:F2}");
                    details.AppendLine($"  Color: ({light.Light.Red:F2}, {light.Light.Green:F2}, {light.Light.Blue:F2})");
                    details.AppendLine($"  Color Scale: {light.Light.ColorScale:F2}");
                    details.AppendLine($"  Animation Type: {light.Light.AnimationTypeEnum}");
                    details.AppendLine($"  Animation Speed 1: {light.Light.AnimationSpeedScale1:F2}");
                    details.AppendLine($"  Animation Speed 2: {light.Light.AnimationSpeedScale2:F2}");
                    details.AppendLine($"  Color Min: {light.Light.ColorMin:F2}");
                    details.AppendLine($"  Random Offset: {light.Light.RandomOffset:F2}");
                    details.AppendLine($"  WPLFT Type: {light.Light.WPLFTType}");
                    details.AppendLine($"  Height Mode: {light.Light.HeightMode}");
                    details.AppendLine($"  For Light Probe Only: {light.Light.ForLightProbeOnly}");
                    break;

                case SpotLightInfoViewModel spotLight:
                    details.AppendLine("Spot Light Details:");
                    details.AppendLine($"  Version: {spotLight.Light.Version}");
                    details.AppendLine($"  Position: ({spotLight.Light.Position.X:F2}, {spotLight.Light.Position.Y:F2}, {spotLight.Light.Position.Z:F2})");
                    details.AppendLine($"  Length: {spotLight.Light.Length:F2}");
                    details.AppendLine($"  Inner Angle: {spotLight.Light.InnerAngleRadians:F2}");
                    details.AppendLine($"  Outer Angle: {spotLight.Light.OuterAngleRadians:F2}");
                    details.AppendLine($"  Color: ({spotLight.Light.IntensityRed:F2}, {spotLight.Light.IntensityGreen:F2}, {spotLight.Light.IntensityBlue:F2})");
                    break;

                case SoundInfoViewModel sound:
                    details.AppendLine("Sound Details:");
                    details.AppendLine($"  Version: {sound.Sound.Version}");
                    details.AppendLine($"  Sound String: {sound.Sound.SoundString}");
                    details.AppendLine($"  Type: {sound.Sound.TypeString}");
                    details.AppendLine($"  Inner Radius: {sound.Sound.InnerRadius:F2}");
                    details.AppendLine($"  Outer Radius: {sound.Sound.OuterRadius:F2}");
                    details.AppendLine($"  Coord Count: {sound.Sound.CoordList.Length}");
                    details.AppendLine($"  Clamp To Surface: {sound.Sound.ClampToSurface}");
                    details.AppendLine($"  Height Mode: {sound.Sound.HeightMode}");
                    details.AppendLine($"  Campaign Type Mask: {sound.Sound.CampaignTypeMask}");
                    details.AppendLine($"  Direction Vector: ({sound.Sound.DirectionVector.X:F2}, {sound.Sound.DirectionVector.Y:F2}, {sound.Sound.DirectionVector.Z:F2})");
                    details.AppendLine($"  Up Vector: ({sound.Sound.UpVector.X:F2}, {sound.Sound.UpVector.Y:F2}, {sound.Sound.UpVector.Z:F2})");
                    details.AppendLine($"  Scope: {sound.Sound.Scope}");
                    break;

                case PolyMeshInfoViewModel mesh:
                    details.AppendLine("PolyMesh Details:");
                    details.AppendLine($"  Version: {mesh.Mesh.PolyMeshVersion}");
                    details.AppendLine($"  Material: {mesh.Mesh.MaterialString}");
                    details.AppendLine($"  Vertices: {mesh.Mesh.VertexList.Length}");
                    details.AppendLine($"  Triangles: {mesh.Mesh.TriangleList.Length / 3}");
                    details.AppendLine($"  Visible In Tactical: {mesh.Mesh.VisibleInTactical}");
                    break;

                case LightProbeInfoViewModel probe:
                    details.AppendLine("Light Probe Details:");
                    details.AppendLine($"  Version: {probe.Probe.Version}");
                    details.AppendLine($"  Position: ({probe.Probe.Position.X:F2}, {probe.Probe.Position.Y:F2}, {probe.Probe.Position.Z:F2})");
                    details.AppendLine($"  Inner Radius: {probe.Probe.InnerRadius:F2}");
                    details.AppendLine($"  Outer Radius: {probe.Probe.OuterRadius:F2}");
                    details.AppendLine($"  Primary: {probe.Probe.Primary}");
                    details.AppendLine($"  Height Mode: {probe.Probe.HeightMode}");
                    break;

                case TerrainHoleInfoViewModel hole:
                    details.AppendLine("Terrain Hole Details:");
                    details.AppendLine($"  Version: {hole.Hole.TerrainHoleVersion}");
                    details.AppendLine($"  Position: ({hole.Hole.FirstVert.X:F2}, {hole.Hole.FirstVert.Y:F2}, {hole.Hole.FirstVert.Z:F2})");
                    break;

                case CscInfoViewModel csc:
                    details.AppendLine("CSC Details:");
                    details.AppendLine($"  Version: {csc.Csc.Version}");
                    details.AppendLine($"  Scene File: {csc.Csc.SceneFile}");
                    details.AppendLine($"  Visible In Shroud: {csc.Csc.VisibleInShroud}");
                    break;

                case BmdInfoViewModel bmd:
                    details.AppendLine("BMD Details:");
                    details.AppendLine($"  Version: {bmd.Bmd.Version}");
                    details.AppendLine($"  BMD String: {bmd.Bmd.BmdString}");
                    details.AppendLine($"  Region: {bmd.Bmd.RegionString}");
                    details.AppendLine($"  Height Mode: {bmd.Bmd.HeightMode}");
                    break;

                case BattlefieldBuildingViewModel building:
                    details.AppendLine("Battlefield Building Details:");
                    details.AppendLine($"  Version: {building.Building.Version}");
                    details.AppendLine($"  Building Key: {building.Building.BuildingKey}");
                    details.AppendLine($"  Building Id: {building.Building.BuildingId}");
                    details.AppendLine($"  Position Type: {building.Building.PositionType}");
                    details.AppendLine($"  Height Mode: {building.Building.HeightMode}");
                    details.AppendLine($"  Parent Id: {building.Building.ParentId}");
                    details.AppendLine($"  Uid: {building.Building.Uid}");
                    break;

                case BattlefieldBuildingFarViewModel buildingFar:
                    details.AppendLine("Battlefield Building Far Details:");
                    details.AppendLine($"  Version: {buildingFar.BuildingFar.Version}");
                    break;

                case CaptureLocationViewModel captureLocation:
                    details.AppendLine("Capture Location Details:");
                    details.AppendLine($"  Version: {captureLocation.CaptureLocation.Version}");
                    break;

                case EFLineViewModel efLine:
                    details.AppendLine("EF Line Details:");
                    details.AppendLine($"  Version: {efLine.EFLine.Version}");
                    break;

                case GoOutlineViewModel goOutline:
                    details.AppendLine("GO Outline Details:");
                    details.AppendLine($"  Version: {goOutline.GoOutline.Version}");
                    break;

                case NonTerrainOutlineViewModel nonTerrainOutline:
                    details.AppendLine("Non-Terrain Outline Details:");
                    details.AppendLine($"  Vertices: {nonTerrainOutline.NonTerrainOutline.VertexList.Count}");
                    break;

                case BuildingProjectileEmitterViewModel emitter:
                    details.AppendLine("Building Projectile Emitter Details:");
                    details.AppendLine($"  Version: {emitter.BuildingProjectileEmitter.BuildingProjectileEmitterVersion}");
                    details.AppendLine($"  Location: ({emitter.BuildingProjectileEmitter.Location.X:F2}, {emitter.BuildingProjectileEmitter.Location.Y:F2}, {emitter.BuildingProjectileEmitter.Location.Z:F2})");
                    details.AppendLine($"  Key: {emitter.BuildingProjectileEmitter.SpecializedBuildingProjectileEmitterKey}");
                    break;

                case ZonesTemplateViewModel zonesTemplate:
                    details.AppendLine("Zones Template Details:");
                    details.AppendLine($"  Version: {zonesTemplate.ZonesTemplate.Version}");
                    details.AppendLine($"  Outline Points: {zonesTemplate.ZonesTemplate.Outline.Count}");
                    break;

                case PlayableAreaViewModel playableArea:
                    details.AppendLine("Playable Area Details:");
                    details.AppendLine($"  Version: {playableArea.PlayableArea.PlayableAreaVersion}");
                    details.AppendLine($"  Has Been Set: {playableArea.PlayableArea.HasBeenSet}");
                    details.AppendLine($"  Bounding Box: ({playableArea.PlayableArea.BoundingBox[0]:F2}, {playableArea.PlayableArea.BoundingBox[1]:F2}, {playableArea.PlayableArea.BoundingBox[2]:F2}, {playableArea.PlayableArea.BoundingBox[3]:F2})");
                    details.AppendLine($"  Flag Version: {playableArea.PlayableArea.FlagVersion}");
                    details.AppendLine($"  Flag 1: {playableArea.PlayableArea.Flag1}");
                    details.AppendLine($"  Flag 2: {playableArea.PlayableArea.Flag2}");
                    details.AppendLine($"  Flag 3: {playableArea.PlayableArea.Flag3}");
                    details.AppendLine($"  Flag 4: {playableArea.PlayableArea.Flag4}");
                    break;

                case DeploymentViewModel deployment:
                    details.AppendLine("Deployment Details:");
                    details.AppendLine($"  Version: {deployment.Deployment.Version}");
                    details.AppendLine($"  Category: {deployment.Deployment.Category}");
                    details.AppendLine($"  Deployment Zones: {deployment.Deployment.DeploymentZones.Count}");
                    break;

                case DeploymentZoneViewModel deploymentZone:
                    details.AppendLine("Deployment Zone Details:");
                    details.AppendLine($"  Version: {deploymentZone.DeploymentZone.Version}");
                    details.AppendLine($"  Deployment Zone Regions: {deploymentZone.DeploymentZone.DeploymentZoneRegions.Count}");
                    break;

                case DeploymentZoneRegionViewModel deploymentZoneRegion:
                    details.AppendLine("Deployment Zone Region Details:");
                    details.AppendLine($"  Version: {deploymentZoneRegion.DeploymentZoneRegion.Version}");
                    details.AppendLine($"  Orientation: {deploymentZoneRegion.DeploymentZoneRegion.Orientation}");
                    details.AppendLine($"  Snap Facing: {deploymentZoneRegion.DeploymentZoneRegion.SnapFacing}");
                    details.AppendLine($"  Id: {deploymentZoneRegion.DeploymentZoneRegion.Id}");
                    details.AppendLine($"  Boundaries: {deploymentZoneRegion.DeploymentZoneRegion.Boundaries.Count}");
                    break;

                case BoundaryViewModel boundary:
                    details.AppendLine("Boundary Details:");
                    details.AppendLine($"  Version: {boundary.Boundary.Version}");
                    details.AppendLine($"  Boundary Type: {boundary.Boundary.BoundaryType}");
                    details.AppendLine($"  Points: {boundary.Boundary.PointList.Count}");
                    if (boundary.Boundary.PointList.Count > 0)
                    {
                        details.AppendLine("  First few points:");
                        for (int i = 0; i < Math.Min(3, boundary.Boundary.PointList.Count); i++)
                        {
                            var point = boundary.Boundary.PointList[i];
                            details.AppendLine($"    Point {i + 1}: ({point.X:F2}, {point.Y:F2})");
                        }
                        if (boundary.Boundary.PointList.Count > 3)
                        {
                            details.AppendLine($"    ... and {boundary.Boundary.PointList.Count - 3} more points");
                        }
                    }
                    break;

                default:
                    details.AppendLine("No additional details available for this component type.");
                    break;
            }

            return details.ToString();
        }

        public void Close() 
        {
            // Clear any active highlights
            _bmdSceneCreator.ClearHighlight();
            
            // Cleanup resources
            AllElements.Clear();
            BattlefieldBuildings.Clear();
            BattlefieldBuildingFars.Clear();
            CaptureLocations.Clear();
            EFLines.Clear();
            GoOutlines.Clear();
            NonTerrainOutlines.Clear();
            BuildingProjectileEmitters.Clear();
            ZonesTemplates.Clear();
            BmdInfos.Clear();
            Props.Clear();
            VfxInfos.Clear();
            PointLights.Clear();
            SpotLights.Clear();
            Sounds.Clear();
            PolyMeshes.Clear();
            LightProbes.Clear();
            TerrainHoles.Clear();
            PlayableAreas.Clear();
            CscInfos.Clear();
            Deployments.Clear();
        }

        private void OnSelectionChanged(SelectionChangedEvent selectionEvent)
        {
            System.Diagnostics.Debug.WriteLine($"OnSelectionChanged called: {selectionEvent.NewState?.GetType().Name}");
            
            if (selectionEvent.NewState is ObjectSelectionState objectSelection)
            {
                var selectedObject = objectSelection.GetSingleSelectedObject();
                System.Diagnostics.Debug.WriteLine($"OnSelectionChanged: Selected object is '{selectedObject?.Name ?? "null"}'");
                
                if (selectedObject != null)
                {
                    // Find the corresponding BMD element for this scene node
                    var component = FindComponentBySceneNode(selectedObject);
                    if (component != null)
                    {
                        // Update the selected component in the UI without triggering another selection change
                        SelectedComponent = component;
                        ComponentDetails = GenerateComponentDetails(component);
                        
                        // Add visible debug info
                        ComponentDetails += $"\n\n[DEBUG] 3D view selected: {component.ElementType} - {component.DisplayName}";
                        
                        System.Diagnostics.Debug.WriteLine($"OnSelectionChanged: 3D view selected component: {component.ElementType} - {component.DisplayName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"OnSelectionChanged: No component found for selected object: {selectedObject.Name}");
                        
                        // Show debug info even when no component is found
                        if (SelectedComponent != null)
                        {
                            ComponentDetails += $"\n\n[DEBUG] 3D view selected object: {selectedObject.Name} (no matching component found)";
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSelectionChanged: New state is not ObjectSelectionState");
                
                if (SelectedComponent != null)
                {
                    ComponentDetails += $"\n\n[DEBUG] Selection changed but not ObjectSelectionState: {selectionEvent.NewState?.GetType().Name}";
                }
            }
        }

        private BmdElementViewModel? FindComponentBySceneNode(ISelectable sceneNode)
        {
            System.Diagnostics.Debug.WriteLine($"FindComponentBySceneNode: Looking for scene node '{sceneNode.Name}'");
            
            // Direct lookup using the ComponentNodes dictionary
            foreach (var kvp in _bmdSceneCreator.ComponentNodes)
            {
                var component = kvp.Key;
                var node = kvp.Value;
                
                // Check if this node or any of its children match the selected scene node
                if (IsNodeOrDescendant(node, sceneNode))
                {
                    System.Diagnostics.Debug.WriteLine($"FindComponentBySceneNode: Found match: {component.ElementType} - {component.DisplayName}");
                    return component;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"FindComponentBySceneNode: No matching component found for scene node '{sceneNode.Name}' or its descendants");
            return null;
        }

        private bool IsNodeOrDescendant(ISceneNode node, ISelectable target)
        {
            // Check if the node itself is the target
            if (node == target)
                return true;
            
            // Check if any descendant matches the target
            foreach (var child in node.Children)
            {
                if (IsNodeOrDescendant(child, target))
                    return true;
            }
            
            return false;
        }


        public void Dispose()
        {
            // Clear any active highlights
            _bmdSceneCreator.ClearHighlight();
            
            // Unregister from events
            _eventHub?.UnRegister(this);
        }
    }

    // Base class for all BMD element view models
    public abstract class BmdElementViewModel : NotifyPropertyChangedImpl
    {
        public string ElementType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public virtual ObservableCollection<BmdElementViewModel> Children { get; } = new();

        protected BmdElementViewModel(string elementType, string displayName, string description = "")
        {
            ElementType = elementType;
            DisplayName = displayName;
            Description = description;
        }
    }

    // View models for specific element types
    public class BattlefieldBuildingViewModel : BmdElementViewModel
    {
        public BattlefieldBuilding Building { get; }

        public BattlefieldBuildingViewModel(BattlefieldBuilding building) 
            : base("Battlefield Building", building.BuildingKey, $"Version: {building.Version}")
        {
            Building = building;
        }
    }

    public class PropInfoViewModel : BmdElementViewModel
    {
        public PropInfo Prop { get; }
        public string PropName { get; }
        public string PropFilePath { get; }

        public PropInfoViewModel(PropInfo prop, string propFilePath) 
            : base("Prop", System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop", $"Version: {prop.PropInfoVersion}")
        {
            Prop = prop;
            PropName = System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop";
            PropFilePath = propFilePath;
        }
    }

    public class VfxInfoViewModel : BmdElementViewModel
    {
        public VfxInfo Vfx { get; }

        public VfxInfoViewModel(VfxInfo vfx) 
            : base("VFX", vfx.VfxString, $"Version: {vfx.VfxInfoVersion}")
        {
            Vfx = vfx;
        }
    }

    public class PointLightInfoViewModel : BmdElementViewModel
    {
        public PointLightInfo Light { get; }

        public PointLightInfoViewModel(PointLightInfo light) 
            : base("Point Light", $"Point Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})", 
                  $"Radius: {light.Radius:F1}, Color: ({light.Red:F1}, {light.Green:F1}, {light.Blue:F1})")
        {
            Light = light;
        }
    }

    public class SpotLightInfoViewModel : BmdElementViewModel
    {
        public SpotLightInfo Light { get; }

        public SpotLightInfoViewModel(SpotLightInfo light) 
            : base("Spot Light", $"Spot Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})", 
                  $"RGB: ({light.IntensityRed:F2},{light.IntensityGreen:F2},{light.IntensityBlue:F2}), Length: {light.Length:F2}")
        {
            Light = light;
        }
    }

    public class SoundInfoViewModel : BmdElementViewModel
    {
        public SoundInfo Sound { get; }

        public SoundInfoViewModel(SoundInfo sound) 
            : base("Sound", sound.SoundString, $"Type: {sound.TypeString}, Version: {sound.Version}")
        {
            Sound = sound;
        }
    }

    public class PolyMeshInfoViewModel : BmdElementViewModel
    {
        public PolyMeshInfo Mesh { get; }

        public PolyMeshInfoViewModel(PolyMeshInfo mesh) 
            : base("PolyMesh", mesh.MaterialString, $"Vertices: {mesh.VertexList.Length}, Triangles: {mesh.TriangleList.Length / 3}")
        {
            Mesh = mesh;
        }
    }

    public class LightProbeInfoViewModel : BmdElementViewModel
    {
        public LightProbeInfo Probe { get; }

        public LightProbeInfoViewModel(LightProbeInfo probe) 
            : base("Light Probe", $"Probe_{probe.Position.X:F2}_{probe.Position.Y:F2}_{probe.Position.Z:F2}", 
                  $"Inner: {probe.InnerRadius:F2}, Outer: {probe.OuterRadius:F2}, Primary: {probe.Primary}")
        {
            Probe = probe;
        }
    }

    public class TerrainHoleInfoViewModel : BmdElementViewModel
    {
        public TerrainHoleTriangleInfo Hole { get; }

        public TerrainHoleInfoViewModel(TerrainHoleTriangleInfo hole) 
            : base("Terrain Hole", $"Hole at ({hole.FirstVert.X:F1}, {hole.FirstVert.Y:F1}, {hole.FirstVert.Z:F1})", 
                  $"Version: {hole.TerrainHoleVersion}")
        {
            Hole = hole;
        }
    }

    public class CscInfoViewModel : BmdElementViewModel
    {
        public CscInfo Csc { get; }

        public CscInfoViewModel(CscInfo csc) 
            : base("CSC Info", csc.SceneFile, $"Version: {csc.Version}")
        {
            Csc = csc;
        }
    }

    public class BattlefieldBuildingFarViewModel : BmdElementViewModel
    {
        public BattlefieldBuildingFar BuildingFar { get; }

        public BattlefieldBuildingFarViewModel(BattlefieldBuildingFar buildingFar) 
            : base("Battlefield Building Far", "", $"Version: {buildingFar.Version}")
        {
            BuildingFar = buildingFar;
        }
    }

    public class CaptureLocationViewModel : BmdElementViewModel
    {
        public CaptureLocation CaptureLocation { get; }

        public CaptureLocationViewModel(CaptureLocation captureLocation) 
            : base("Capture Location", "", $"Version: {captureLocation.Version}")
        {
            CaptureLocation = captureLocation;
        }
    }

    public class EFLineViewModel : BmdElementViewModel
    {
        public EFLine EFLine { get; }

        public EFLineViewModel(EFLine efLine) 
            : base("EF Line", "", $"Version: {efLine.Version}")
        {
            EFLine = efLine;
        }
    }

    public class GoOutlineViewModel : BmdElementViewModel
    {
        public GoOutline GoOutline { get; }

        public GoOutlineViewModel(GoOutline goOutline) 
            : base("GO Outline", "", $"Version: {goOutline.Version}")
        {
            GoOutline = goOutline;
        }
    }

    public class NonTerrainOutlineViewModel : BmdElementViewModel
    {
        public NonTerrainOutline NonTerrainOutline { get; }

        public NonTerrainOutlineViewModel(NonTerrainOutline nonTerrainOutline) 
            : base("Non-Terrain Outline", "", $"Vertices: {nonTerrainOutline.VertexList?.Count ?? 0}")
        {
            NonTerrainOutline = nonTerrainOutline;
        }
    }

    public class BuildingProjectileEmitterViewModel : BmdElementViewModel
    {
        public BuildingProjectileEmitter BuildingProjectileEmitter { get; }

        public BuildingProjectileEmitterViewModel(BuildingProjectileEmitter buildingProjectileEmitter) 
            : base("Building Projectile Emitter", buildingProjectileEmitter.SpecializedBuildingProjectileEmitterKey, $"Version: {buildingProjectileEmitter.BuildingProjectileEmitterVersion}")
        {
            BuildingProjectileEmitter = buildingProjectileEmitter;
        }
    }

    public class ZonesTemplateViewModel : BmdElementViewModel
    {
        public ZonesTemplate ZonesTemplate { get; }

        public ZonesTemplateViewModel(ZonesTemplate zonesTemplate) 
            : base("Zones Template", "", $"Version: {zonesTemplate.Version}")
        {
            ZonesTemplate = zonesTemplate;
        }
    }

    public class PlayableAreaViewModel : BmdElementViewModel
    {
        public PlayableArea PlayableArea { get; }

        public PlayableAreaViewModel(PlayableArea playableArea) 
            : base("Playable Area", "", $"Version: {playableArea.PlayableAreaVersion}")
        {
            PlayableArea = playableArea;
        }
    }

    public class BmdInfoViewModel : BmdElementViewModel
    {
        public BmdInfo Bmd { get; }
        public ObservableCollection<BmdElementViewModel> ChildElements { get; } = new();
        public bool IsExpanded { get; set; } = false;
        public bool HasChildren { get; set; } = false;

        public override ObservableCollection<BmdElementViewModel> Children => ChildElements;

        public BmdInfoViewModel(BmdInfo bmd) 
            : base("BMD", bmd.BmdString, $"Version: {bmd.Version}, Region: {bmd.RegionString}")
        {
            Bmd = bmd;
        }
    }

    public class DeploymentZoneViewModel : BmdElementViewModel
    {
        public DeploymentZone DeploymentZone { get; }

        public DeploymentZoneViewModel(DeploymentZone deploymentZone, int index) 
            : base("Deployment Zone", $"Zone {index + 1}", $"Version: {deploymentZone.Version}, Regions: {deploymentZone.DeploymentZoneRegions.Count}")
        {
            DeploymentZone = deploymentZone;
            
            // Add child regions
            foreach (var region in deploymentZone.DeploymentZoneRegions)
            {
                Children.Add(new DeploymentZoneRegionViewModel(region));
            }
        }
    }

    public class DeploymentZoneRegionViewModel : BmdElementViewModel
    {
        public DeploymentZoneRegion DeploymentZoneRegion { get; }

        public DeploymentZoneRegionViewModel(DeploymentZoneRegion deploymentZoneRegion) 
            : base("Deployment Zone Region", $"Region {deploymentZoneRegion.Id}", $"Version: {deploymentZoneRegion.Version}, Boundaries: {deploymentZoneRegion.Boundaries.Count}")
        {
            DeploymentZoneRegion = deploymentZoneRegion;
            
            // Add child boundaries
            foreach (var boundary in deploymentZoneRegion.Boundaries)
            {
                Children.Add(new BoundaryViewModel(boundary));
            }
        }
    }

    public class BoundaryViewModel : BmdElementViewModel
    {
        public Boundary Boundary { get; }

        public BoundaryViewModel(Boundary boundary) 
            : base("Boundary", boundary.BoundaryType, $"Version: {boundary.Version}, Points: {boundary.PointList.Count}")
        {
            Boundary = boundary;
        }
    }

    public class DeploymentViewModel : BmdElementViewModel
    {
        public Deployment Deployment { get; }

        public DeploymentViewModel(Deployment deployment) 
            : base("Deployment", deployment.Category, $"Version: {deployment.Version}, Zones: {deployment.DeploymentZones.Count}")
        {
            Deployment = deployment;
            
            // Add child zones
            for (int i = 0; i < deployment.DeploymentZones.Count; i++)
            {
                Children.Add(new DeploymentZoneViewModel(deployment.DeploymentZones[i], i));
            }
        }
    }
}
