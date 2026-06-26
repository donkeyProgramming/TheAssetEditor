using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.BmdEditor.Services;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.WpfWindow;
using Shared.Core.Commands;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Bmd;

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
        public ObservableCollection<BmdElementViewModel> AllElements { get; } = [];
        public ObservableCollection<BattlefieldBuildingViewModel> BattlefieldBuildings { get; } = [];
        public ObservableCollection<BattlefieldBuildingFarViewModel> BattlefieldBuildingFars { get; } = [];
        public ObservableCollection<CaptureLocationViewModel> CaptureLocations { get; } = [];
        public ObservableCollection<EFLineViewModel> EFLines { get; } = [];
        public ObservableCollection<GoOutlineViewModel> GoOutlines { get; } = [];
        public ObservableCollection<NonTerrainOutlineViewModel> NonTerrainOutlines { get; } = [];
        public ObservableCollection<BuildingProjectileEmitterViewModel> BuildingProjectileEmitters { get; } = [];
        public ObservableCollection<ZonesTemplateViewModel> ZonesTemplates { get; } = [];
        public ObservableCollection<BmdInfoViewModel> BmdInfos { get; } = [];
        public ObservableCollection<PropInfoViewModel> Props { get; } = [];
        public ObservableCollection<VfxInfoViewModel> VfxInfos { get; } = [];
        public ObservableCollection<PointLightInfoViewModel> PointLights { get; } = [];
        public ObservableCollection<SpotLightInfoViewModel> SpotLights { get; } = [];
        public ObservableCollection<SoundInfoViewModel> Sounds { get; } = [];
        public ObservableCollection<PolyMeshInfoViewModel> PolyMeshes { get; } = [];
        public ObservableCollection<LightProbeInfoViewModel> LightProbes { get; } = [];
        public ObservableCollection<TerrainHoleInfoViewModel> TerrainHoles { get; } = [];
        public ObservableCollection<PlayableAreaViewModel> PlayableAreas { get; } = [];
        public ObservableCollection<CscInfoViewModel> CscInfos { get; } = [];
        public ObservableCollection<DeploymentViewModel> Deployments { get; } = [];

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
        private readonly SelectionManager? _selectionManager;
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
            _selectionManager = selectionManager!;
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
                    var openCommand = new OpenEditorCommand(_editorCreator, _packFileService);
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
                    var objectSelection = _selectionManager!.GetState<ObjectSelectionState>();
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

        private static ISelectable? FindFirstSelectableNode(ISceneNode sceneNode)
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

        private static string GenerateComponentDetails(BmdElementViewModel component)
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
                    details.AppendLine($"  Flag Version: {vfx.Vfx.Flags.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {vfx.Vfx.Flags.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Water: {vfx.Vfx.Flags.ClampToWaterSurface}");
                    details.AppendLine($"  Visible In Tactical: {vfx.Vfx.Flags.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {vfx.Vfx.Flags.OnlyVisibleInTactical}");
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
                    details.AppendLine($"  Falloff Type: {light.Light.FalloffType}");
                    details.AppendLine($"  Height Mode: {light.Light.HeightMode}");
                    details.AppendLine($"  Light Probe Only: {light.Light.LightProbeOnly}");
                    details.AppendLine($"  Flags Version: {light.Light.Flags.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {light.Light.Flags.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Surface: {light.Light.Flags.ClampToSurface}");
                    details.AppendLine($"  Clamp To Water Surface: {light.Light.Flags.ClampToWaterSurface}");
                    details.AppendLine($"  Season Spring: {light.Light.Flags.SeasonSpring}");
                    details.AppendLine($"  Season Summer: {light.Light.Flags.SeasonSummer}");
                    details.AppendLine($"  Season Autumn: {light.Light.Flags.SeasonAutumn}");
                    details.AppendLine($"  Season Winter: {light.Light.Flags.SeasonWinter}");
                    details.AppendLine($"  Visible In Tactical: {light.Light.Flags.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {light.Light.Flags.OnlyVisibleInTactical}");
                    break;

                case SpotLightInfoViewModel spotLight:
                    details.AppendLine("Spot Light Details:");
                    details.AppendLine($"  Version: {spotLight.Light.Version}");
                    details.AppendLine($"  Position: ({spotLight.Light.Position.X:F2}, {spotLight.Light.Position.Y:F2}, {spotLight.Light.Position.Z:F2})");
                    details.AppendLine($"  Length: {spotLight.Light.Length:F2}");
                    details.AppendLine($"  Inner Angle: {spotLight.Light.InnerAngleRadians:F2}");
                    details.AppendLine($"  Outer Angle: {spotLight.Light.OuterAngleRadians:F2}");
                    details.AppendLine($"  Color: ({spotLight.Light.IntensityRed:F2}, {spotLight.Light.IntensityGreen:F2}, {spotLight.Light.IntensityBlue:F2})");
                    details.AppendLine($"  PdlcMask: {spotLight.Light.PdlcMask}");
                    details.AppendLine($"  Flags Version: {spotLight.Light.Flags.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {spotLight.Light.Flags.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Surface: {spotLight.Light.Flags.ClampToSurface}");
                    details.AppendLine($"  Clamp To Water Surface: {spotLight.Light.Flags.ClampToWaterSurface}");
                    details.AppendLine($"  Season Spring: {spotLight.Light.Flags.SeasonSpring}");
                    details.AppendLine($"  Season Summer: {spotLight.Light.Flags.SeasonSummer}");
                    details.AppendLine($"  Season Autumn: {spotLight.Light.Flags.SeasonAutumn}");
                    details.AppendLine($"  Season Winter: {spotLight.Light.Flags.SeasonWinter}");
                    details.AppendLine($"  Visible In Tactical: {spotLight.Light.Flags.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {spotLight.Light.Flags.OnlyVisibleInTactical}");
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
                    details.AppendLine($"  Flags Version: {mesh.Mesh.Flags.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {mesh.Mesh.Flags.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Surface: {mesh.Mesh.Flags.ClampToSurface}");
                    details.AppendLine($"  Clamp To Water Surface: {mesh.Mesh.Flags.ClampToWaterSurface}");
                    details.AppendLine($"  Season Spring: {mesh.Mesh.Flags.SeasonSpring}");
                    details.AppendLine($"  Season Summer: {mesh.Mesh.Flags.SeasonSummer}");
                    details.AppendLine($"  Season Autumn: {mesh.Mesh.Flags.SeasonAutumn}");
                    details.AppendLine($"  Season Winter: {mesh.Mesh.Flags.SeasonWinter}");
                    details.AppendLine($"  Visible In Tactical: {mesh.Mesh.Flags.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {mesh.Mesh.Flags.OnlyVisibleInTactical}");
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
                    details.AppendLine($"  Flags Version: {hole.Hole.Flags.FlagVersion}");
                    details.AppendLine($"  Allow In Outfield: {hole.Hole.Flags.AllowInOutfield}");
                    details.AppendLine($"  Clamp To Surface: {hole.Hole.Flags.ClampToSurface}");
                    details.AppendLine($"  Clamp To Water Surface: {hole.Hole.Flags.ClampToWaterSurface}");
                    details.AppendLine($"  Season Spring: {hole.Hole.Flags.SeasonSpring}");
                    details.AppendLine($"  Season Summer: {hole.Hole.Flags.SeasonSummer}");
                    details.AppendLine($"  Season Autumn: {hole.Hole.Flags.SeasonAutumn}");
                    details.AppendLine($"  Season Winter: {hole.Hole.Flags.SeasonWinter}");
                    details.AppendLine($"  Visible In Tactical: {hole.Hole.Flags.VisibleInTactical}");
                    details.AppendLine($"  Only Visible In Tactical: {hole.Hole.Flags.OnlyVisibleInTactical}");
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
                    details.AppendLine($"  Vertices: {goOutline.GoOutline.VertexList.Count}");
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
                        for (var i = 0; i < Math.Min(3, boundary.Boundary.PointList.Count); i++)
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

        private static bool IsNodeOrDescendant(ISceneNode node, ISelectable target)
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
            GC.SuppressFinalize(this);
        }
    }

    // Base class for all BMD element view models
    public abstract class BmdElementViewModel(string elementType, string displayName, string description = "") : NotifyPropertyChangedImpl
    {
        public string ElementType { get; } = elementType;
        public string DisplayName { get; } = displayName;
        public string Description { get; } = description;
        public virtual ObservableCollection<BmdElementViewModel> Children { get; } = [];
    }

    // View models for specific element types
    public class BattlefieldBuildingViewModel(BattlefieldBuilding building) : BmdElementViewModel("Battlefield Building", building.BuildingKey, $"Version: {building.Version}")
    {
        public BattlefieldBuilding Building { get; } = building;
    }

    public class PropInfoViewModel(PropInfo prop, string propFilePath) : BmdElementViewModel("Prop", System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop", $"Version: {prop.PropInfoVersion}")
    {
        public PropInfo Prop { get; } = prop;
        public string PropName { get; } = System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop";
        public string PropFilePath { get; } = propFilePath;
    }

    public class VfxInfoViewModel(VfxInfo vfx) : BmdElementViewModel("VFX", vfx.VfxString, $"Version: {vfx.VfxInfoVersion}")
    {
        public VfxInfo Vfx { get; } = vfx;
    }

    public class PointLightInfoViewModel(PointLightInfo light) : BmdElementViewModel("Point Light", $"Point Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})", 
          $"Radius: {light.Radius:F1}, Color: ({light.Red:F1}, {light.Green:F1}, {light.Blue:F1})")
    {
        public PointLightInfo Light { get; } = light;
    }

    public class SpotLightInfoViewModel(SpotLightInfo light) : BmdElementViewModel("Spot Light", $"Spot Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})", 
          $"RGB: ({light.IntensityRed:F2},{light.IntensityGreen:F2},{light.IntensityBlue:F2}), Length: {light.Length:F2}")
    {
        public SpotLightInfo Light { get; } = light;
    }

    public class SoundInfoViewModel(SoundInfo sound) : BmdElementViewModel("Sound", sound.SoundString, $"Type: {sound.TypeString}, Version: {sound.Version}")
    {
        public SoundInfo Sound { get; } = sound;
    }

    public class PolyMeshInfoViewModel(PolyMeshInfo mesh) : BmdElementViewModel("PolyMesh", mesh.MaterialString, $"Vertices: {mesh.VertexList.Length}, Triangles: {mesh.TriangleList.Length / 3}")
    {
        public PolyMeshInfo Mesh { get; } = mesh;
    }

    public class LightProbeInfoViewModel(LightProbeInfo probe) : BmdElementViewModel("Light Probe", $"Probe_{probe.Position.X:F2}_{probe.Position.Y:F2}_{probe.Position.Z:F2}", 
          $"Inner: {probe.InnerRadius:F2}, Outer: {probe.OuterRadius:F2}, Primary: {probe.Primary}")
    {
        public LightProbeInfo Probe { get; } = probe;
    }

    public class TerrainHoleInfoViewModel(TerrainHoleTriangleInfo hole) : BmdElementViewModel("Terrain Hole", $"Hole at ({hole.FirstVert.X:F1}, {hole.FirstVert.Y:F1}, {hole.FirstVert.Z:F1})", 
          $"Version: {hole.TerrainHoleVersion}")
    {
        public TerrainHoleTriangleInfo Hole { get; } = hole;
    }

    public class CscInfoViewModel(CscInfo csc) : BmdElementViewModel("CSC Info", csc.SceneFile, $"Version: {csc.Version}")
    {
        public CscInfo Csc { get; } = csc;
    }

    public class BattlefieldBuildingFarViewModel(BattlefieldBuildingFar buildingFar) : BmdElementViewModel("Battlefield Building Far", "", $"Version: {buildingFar.Version}")
    {
        public BattlefieldBuildingFar BuildingFar { get; } = buildingFar;
    }

    public class CaptureLocationViewModel(CaptureLocation captureLocation) : BmdElementViewModel("Capture Location", "", $"Version: {captureLocation.Version}")
    {
        public CaptureLocation CaptureLocation { get; } = captureLocation;
    }

    public class EFLineViewModel(EFLine efLine) : BmdElementViewModel("EF Line", "", $"Version: {efLine.Version}")
    {
        public EFLine EFLine { get; } = efLine;
    }

    public class GoOutlineViewModel(GoOutline goOutline) : BmdElementViewModel("Go Outline", "", $"Vertices: {goOutline.VertexList?.Count ?? 0}")
    {
        public GoOutline GoOutline { get; } = goOutline;
    }

    public class NonTerrainOutlineViewModel(NonTerrainOutline nonTerrainOutline) : BmdElementViewModel("Non-Terrain Outline", "", $"Vertices: {nonTerrainOutline.VertexList?.Count ?? 0}")
    {
        public NonTerrainOutline NonTerrainOutline { get; } = nonTerrainOutline;
    }

    public class BuildingProjectileEmitterViewModel(BuildingProjectileEmitter buildingProjectileEmitter) : BmdElementViewModel("Building Projectile Emitter", buildingProjectileEmitter.SpecializedBuildingProjectileEmitterKey, $"Version: {buildingProjectileEmitter.BuildingProjectileEmitterVersion}")
    {
        public BuildingProjectileEmitter BuildingProjectileEmitter { get; } = buildingProjectileEmitter;
    }

    public class ZonesTemplateViewModel(ZonesTemplate zonesTemplate) : BmdElementViewModel("Zones Template", "", "")
    {
        public ZonesTemplate ZonesTemplate { get; } = zonesTemplate;
    }

    public class PlayableAreaViewModel(PlayableArea playableArea) : BmdElementViewModel("Playable Area", "", $"Version: {playableArea.PlayableAreaVersion}")
    {
        public PlayableArea PlayableArea { get; } = playableArea;
    }

    public class BmdInfoViewModel(BmdInfo bmd) : BmdElementViewModel("BMD", bmd.BmdString, $"Version: {bmd.Version}, Region: {bmd.RegionString}")
    {
        public BmdInfo Bmd { get; } = bmd;
        public ObservableCollection<BmdElementViewModel> ChildElements { get; } = [];
        public bool IsExpanded { get; set; } = false;
        public bool HasChildren { get; set; } = false;

        public override ObservableCollection<BmdElementViewModel> Children => ChildElements;
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

    public class BoundaryViewModel(Boundary boundary) : BmdElementViewModel("Boundary", boundary.BoundaryType, $"Version: {boundary.Version}, Points: {boundary.PointList.Count}")
    {
        public Boundary Boundary { get; } = boundary;
    }

    public class DeploymentViewModel : BmdElementViewModel
    {
        public Deployment Deployment { get; }

        public DeploymentViewModel(Deployment deployment) 
            : base("Deployment", deployment.Category, $"Version: {deployment.Version}, Zones: {deployment.DeploymentZones.Count}")
        {
            Deployment = deployment;
            
            // Add child zones
            for (var i = 0; i < deployment.DeploymentZones.Count; i++)
            {
                Children.Add(new DeploymentZoneViewModel(deployment.DeploymentZones[i], i));
            }
        }
    }
}
