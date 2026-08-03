using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.BmdEditor.Exporting;
using Editors.BmdEditor.Services;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.WpfWindow;
using Microsoft.Xna.Framework;
using Shared.Core.Commands;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Bmd;
using Shared.GameFormats.RigidModel.Transforms;

namespace Editors.BmdEditor.ViewModels
{
    public class BmdEditorViewModel : NotifyPropertyChangedImpl, IEditorInterface, IFileEditor, ISaveableEditor, IDisposable
    {
        private readonly IPackFileService _packFileService;
        private readonly IEditorManager _editorCreator;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSaveService _fileSaveService;
        private readonly Shared.Core.Events.IEventHub _eventHub;
        private BmdFile? _bmdFile;

        public string DisplayName { get; set; } = "Not set";
        public PackFile CurrentFile { get; private set; } = null!;
        public string StatusText { get; set; } = "Ready";

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetAndNotify(ref _hasUnsavedChanges, value);
        }

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

        /// <summary>Category sections shown in the (collapsible) component tree - built from the
        /// typed collections above whenever they're (re)loaded.</summary>
        public ObservableCollection<BmdCategoryGroupViewModel> ComponentGroups { get; } = [];

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NavigateToReferencedFileCommand { get; }
        public ICommand GizmoTranslateCommand { get; }
        public ICommand GizmoRotateCommand { get; }
        public ICommand GizmoScaleCommand { get; }
        public ICommand GizmoOffCommand { get; }
        public ICommand AddPropCommand { get; }
        public ICommand AddDecalCommand { get; }
        public ICommand AddVfxCommand { get; }
        public ICommand AddPointLightCommand { get; }
        public ICommand AddSpotLightCommand { get; }
        public ICommand AddSoundCommand { get; }
        public ICommand AddPolyMeshCommand { get; }
        public ICommand AddLightProbeCommand { get; }
        public ICommand AddTerrainHoleCommand { get; }
        public ICommand AddCscCommand { get; }

        // Selected component for details display
        private BmdElementViewModel? _selectedComponent;
        public BmdElementViewModel? SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                SetAndNotify(ref _selectedComponent, value);
                NotifyPropertyChanged(nameof(HasSelection));
            }
        }

        public bool HasSelection => SelectedComponent != null;

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
        private readonly BmdGizmoComponent _gizmo;

        public IWpfGame Scene { get; set; }

        public BmdEditorViewModel(
            IPackFileService packFileService,
            IEditorManager editorCreator,
            IStandardDialogs standardDialogs,
            IFileSystemAccess fileSystemAccess,
            IFileSaveService fileSaveService,
            Shared.Core.Events.IEventHub eventHub,
            IWpfGame gameWorld,
            BmdSceneCreator bmdSceneCreator,
            SelectionManager selectionManager,
            IComponentInserter componentInserter,
            BmdElementLoader bmdElementLoader,
            BmdGizmoComponent gizmoComponent)
        {
            _packFileService = packFileService;
            _editorCreator = editorCreator;
            _standardDialogs = standardDialogs;
            _fileSystemAccess = fileSystemAccess;
            _fileSaveService = fileSaveService;
            _eventHub = eventHub;
            _bmdSceneCreator = bmdSceneCreator;
            _selectionManager = selectionManager;
            _bmdElementLoader = bmdElementLoader;
            _gizmo = gizmoComponent;

            Scene = gameWorld;

            // Ensure all game components are added to the editor
            componentInserter.Execute();

            RefreshCommand = new RelayCommand(Refresh);
            ExportCommand = new RelayCommand(Export);
            SaveCommand = new RelayCommand(() => Save());
            NavigateToReferencedFileCommand = new RelayCommand<string>(NavigateToReferencedFile);
            GizmoTranslateCommand = new RelayCommand(() => _gizmo.SetMode(GameWorld.Core.Components.Gizmo.GizmoMode.Translate));
            GizmoRotateCommand = new RelayCommand(() => _gizmo.SetMode(GameWorld.Core.Components.Gizmo.GizmoMode.Rotate));
            GizmoScaleCommand = new RelayCommand(() => _gizmo.SetMode(GameWorld.Core.Components.Gizmo.GizmoMode.NonUniformScale));
            GizmoOffCommand = new RelayCommand(_gizmo.Disable);
            AddPropCommand = new RelayCommand(AddProp);
            AddDecalCommand = new RelayCommand(AddDecal);
            AddVfxCommand = new RelayCommand(AddVfx);
            AddPointLightCommand = new RelayCommand(AddPointLight);
            AddSpotLightCommand = new RelayCommand(AddSpotLight);
            AddSoundCommand = new RelayCommand(AddSound);
            AddPolyMeshCommand = new RelayCommand(AddPolyMesh);
            AddLightProbeCommand = new RelayCommand(AddLightProbe);
            AddTerrainHoleCommand = new RelayCommand(AddTerrainHole);
            AddCscCommand = new RelayCommand(AddCsc);

            // Subscribe to selection changes from 3D view
            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);
        }

        public void LoadFile(PackFile packFile)
        {
            CurrentFile = packFile;
            DisplayName = packFile.Name;

            var data = packFile.DataSource.ReadData();

            using var stream = new MemoryStream(data);
            var parser = new BmdParser(stream);

            BmdFile = parser.Parse();
            HasUnsavedChanges = false;

            // Create 3D scene structure first
            _bmdSceneCreator.CreateSceneFromBmd(BmdFile!, packFile);

            // Update collections (this will load scene content)
            PopulateViewModels();
        }

        // -------------------------------------------------------------------
        // "Add" menu - creates a default element in the corresponding BmdFile
        // list, then rebuilds the scene/view models from the (already-edited,
        // in-memory) BmdFile - same rebuild the initial load does, just
        // without re-reading from disk - and selects the new element.
        // -------------------------------------------------------------------

        private void FinishAdd(Func<BmdElementViewModel?> findNewElement)
        {
            if (BmdFile == null)
                return;

            _bmdSceneCreator.CreateSceneFromBmd(BmdFile, CurrentFile);
            PopulateViewModels();
            HasUnsavedChanges = true;

            var newElement = findNewElement();
            if (newElement != null)
                SelectComponent(newElement);
        }

        private void AddProp()
        {
            if (BmdFile == null)
                return;
            var result = _standardDialogs.DisplayBrowseDialog([".rigid_model_v2", ".wsmodel"]);
            if (result.Result == false || result.File == null)
                return;

            BmdFile.PropInfos.Add(BmdElementFactory.CreateProp(_packFileService.GetFullPath(result.File)));
            FinishAdd(() => Props.LastOrDefault());
        }

        private void AddDecal()
        {
            if (BmdFile == null)
                return;
            var result = _standardDialogs.DisplayBrowseDialog([".rigid_model_v2", ".wsmodel"]);
            if (result.Result == false || result.File == null)
                return;

            BmdFile.PropInfos.Add(BmdElementFactory.CreateProp(_packFileService.GetFullPath(result.File), isDecal: true));
            FinishAdd(() => Props.LastOrDefault());
        }

        private void AddVfx()
        {
            if (BmdFile == null)
                return;
            var input = _standardDialogs.ShowTextInputDialog("VFX path (e.g. env_forest_dust)");
            if (input.Result == false || string.IsNullOrWhiteSpace(input.Text))
                return;

            BmdFile.VfxInfos.Add(BmdElementFactory.CreateVfx(input.Text.Trim()));
            FinishAdd(() => VfxInfos.LastOrDefault());
        }

        private void AddPointLight()
        {
            if (BmdFile == null)
                return;
            BmdFile.PointLights.Add(BmdElementFactory.CreatePointLight());
            FinishAdd(() => PointLights.LastOrDefault());
        }

        private void AddSpotLight()
        {
            if (BmdFile == null)
                return;
            BmdFile.SpotLights.Add(BmdElementFactory.CreateSpotLight());
            FinishAdd(() => SpotLights.LastOrDefault());
        }

        private void AddSound()
        {
            if (BmdFile == null)
                return;
            var input = _standardDialogs.ShowTextInputDialog("Sound event (e.g. Play_My_Sound)");
            if (input.Result == false || string.IsNullOrWhiteSpace(input.Text))
                return;

            BmdFile.Sounds.Add(BmdElementFactory.CreateSound(input.Text.Trim()));
            FinishAdd(() => Sounds.LastOrDefault());
        }

        private void AddPolyMesh()
        {
            if (BmdFile == null)
                return;
            var input = _standardDialogs.ShowTextInputDialog("Material name");
            if (input.Result == false)
                return;

            BmdFile.PolyMeshes.Add(BmdElementFactory.CreatePolyMesh(input.Text?.Trim() ?? string.Empty));
            FinishAdd(() => PolyMeshes.LastOrDefault());
        }

        private void AddLightProbe()
        {
            if (BmdFile == null)
                return;
            BmdFile.LightProbes.Add(BmdElementFactory.CreateLightProbe());
            FinishAdd(() => LightProbes.LastOrDefault());
        }

        private void AddTerrainHole()
        {
            if (BmdFile == null)
                return;
            BmdFile.TerrainHoles.Add(BmdElementFactory.CreateTerrainHole());
            FinishAdd(() => TerrainHoles.LastOrDefault());
        }

        private void AddCsc()
        {
            if (BmdFile == null)
                return;
            var result = _standardDialogs.DisplayBrowseDialog([".csc"]);
            if (result.Result == false || result.File == null)
                return;

            BmdFile.CscInfos.Add(BmdElementFactory.CreateCsc(_packFileService.GetFullPath(result.File)));
            FinishAdd(() => CscInfos.LastOrDefault());
        }

        public bool Save()
        {
            if (BmdFile == null)
                return false;

            try
            {
                var bytes = BmdWriter.Write(BmdFile);
                var path = _packFileService.GetFullPath(CurrentFile);
                var result = _fileSaveService.Save(path, bytes, prompOnConflict: false);
                if (result != null)
                {
                    CurrentFile = result;
                    HasUnsavedChanges = false;
                    StatusText = $"Saved {result.Name}";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                _standardDialogs.ShowExceptionWindow(e, "Failed to save the BMD file. The file on disk is unchanged.");
                return false;
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

            // Mark the file dirty and push the edited transform back into the 3D view whenever a
            // transform (or other editable) property changes on any of the 10 editable categories.
            foreach (var element in AllElements)
                element.Modified += () => OnElementModified(element);

            RebuildComponentGroups();
        }

        /// <summary>Splits the flat typed collections into named, collapsible sections for the
        /// component tree - including separating decal props from regular props.</summary>
        private void RebuildComponentGroups()
        {
            ComponentGroups.Clear();

            void AddGroup(string header, IReadOnlyCollection<BmdElementViewModel> items)
            {
                if (items.Count > 0)
                    ComponentGroups.Add(new BmdCategoryGroupViewModel($"{header} ({items.Count})", items));
            }

            AddGroup("Props", Props.Where(p => !p.Prop.IsDecal).ToList());
            AddGroup("Decals", Props.Where(p => p.Prop.IsDecal).ToList());
            AddGroup("VFX", VfxInfos);
            AddGroup("Point Lights", PointLights);
            AddGroup("Spot Lights", SpotLights);
            AddGroup("Sounds", Sounds);
            AddGroup("Poly Meshes", PolyMeshes);
            AddGroup("Light Probes", LightProbes);
            AddGroup("Terrain Holes", TerrainHoles);
            AddGroup("Composite Scenes", CscInfos);
            AddGroup("Referenced BMDs", BmdInfos);
            AddGroup("Battlefield Buildings", BattlefieldBuildings);
            AddGroup("Battlefield Building Fars", BattlefieldBuildingFars);
            AddGroup("Capture Locations", CaptureLocations);
            AddGroup("EF Lines", EFLines);
            AddGroup("Go Outlines", GoOutlines);
            AddGroup("Non-Terrain Outlines", NonTerrainOutlines);
            AddGroup("Building Projectile Emitters", BuildingProjectileEmitters);
            AddGroup("Zones Templates", ZonesTemplates);
            AddGroup("Playable Areas", PlayableAreas);
            AddGroup("Deployments", Deployments);
        }

        private void OnElementModified(BmdElementViewModel element)
        {
            HasUnsavedChanges = true;
            _bmdSceneCreator.RefreshVisual(element);
        }

        /// <summary>Marks the file dirty for details-panel fields bound directly to the underlying
        /// domain object (flags, strings, etc.) rather than through a dedicated view-model
        /// property - those plain POCOs don't raise <see cref="BmdElementViewModel.Modified"/>
        /// themselves.</summary>
        public void OnAuxFieldModified() => HasUnsavedChanges = true;

        private void Refresh()
        {
            if (CurrentFile != null)
            {
                LoadFile(CurrentFile);
            }
        }

        private void Export()
        {
            if (BmdFile == null)
                return;

            var folderResult = _standardDialogs.ShowSystemFolderBrowserDialog();
            if (!folderResult.Result || string.IsNullOrWhiteSpace(folderResult.FolderPath))
                return;

            var baseName = Path.GetFileNameWithoutExtension(CurrentFile.Name);
            var project = BmdTerryProjectWriter.Build(BmdFile, BmdReferenceResolver.Create(_packFileService));

            var terryPath = Path.Combine(folderResult.FolderPath, baseName + ".terry");
            var layerPath = Path.Combine(folderResult.FolderPath, $"{baseName}.{project.LayerEntityId}.layer");

            _fileSystemAccess.FileWriteAllBytes(terryPath, Encoding.UTF8.GetBytes(project.TerryXml));
            _fileSystemAccess.FileWriteAllBytes(layerPath, Encoding.UTF8.GetBytes(project.LayerXml));
        }

        private void NavigateToReferencedFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            // Try to find the referenced file in the pack file system
            var referencedFile = _packFileService.FindFile(fileName);
            if (referencedFile != null)
            {
                // Open the referenced file in the appropriate editor
                var openCommand = new OpenEditorCommand(_editorCreator, _packFileService);
                openCommand.Execute(referencedFile);
            }
        }

        public void SelectComponent(BmdElementViewModel component)
        {
            SelectedComponent = component;
            ComponentDetails = GenerateComponentDetails(component);
            _gizmo.SetTarget(component);

            // Select the component in the 3D scene using SelectionManager
            SelectComponentIn3DScene(component);
        }

        private void SelectComponentIn3DScene(BmdElementViewModel component)
        {
            if (_bmdSceneCreator.ComponentNodes.TryGetValue(component, out var sceneNode))
            {
                // Find the first selectable child node (Rmv2MeshNode implements ISelectable)
                var selectableNode = FindFirstSelectableNode(sceneNode);
                if (selectableNode != null)
                {
                    // Clear current selection and select the new object
                    var objectSelection = _selectionManager.GetState<ObjectSelectionState>();
                    if (objectSelection != null)
                    {
                        objectSelection.Clear();
                        objectSelection.ModifySelectionSingleObject(selectableNode, false);
                    }
                }
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
            ComponentGroups.Clear();
        }

        private void OnSelectionChanged(SelectionChangedEvent selectionEvent)
        {
            if (selectionEvent.NewState is ObjectSelectionState objectSelection)
            {
                var selectedObject = objectSelection.GetSingleSelectedObject();
                if (selectedObject != null)
                {
                    // Find the corresponding BMD element for this scene node
                    var component = FindComponentBySceneNode(selectedObject);
                    if (component != null)
                    {
                        // Update the selected component in the UI without triggering another selection change
                        SelectedComponent = component;
                        ComponentDetails = GenerateComponentDetails(component);
                        _gizmo.SetTarget(component);
                    }
                }
            }
        }

        private BmdElementViewModel? FindComponentBySceneNode(ISelectable sceneNode)
        {
            // Direct lookup using the ComponentNodes dictionary
            foreach (var kvp in _bmdSceneCreator.ComponentNodes)
            {
                var component = kvp.Key;
                var node = kvp.Value;

                // Check if this node or any of its children match the selected scene node
                if (IsNodeOrDescendant(node, sceneNode))
                    return component;
            }

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

        /// <summary>Raised whenever a transform (or other editable) property changes the
        /// underlying domain data - the editor subscribes to mark the file dirty and refresh the
        /// 3D view's visual for this element.</summary>
        public event Action? Modified;
        protected void RaiseModified() => Modified?.Invoke();

        // -------------------------------------------------------------------
        // Gizmo support - every one of the 10 editable categories can be
        // translated; rotation/scale are only exposed where the format
        // actually carries that data (see the per-category overrides below).
        // -------------------------------------------------------------------
        public virtual bool SupportsRotate => false;
        public virtual bool SupportsScale => false;
        public virtual Vector3 GizmoPosition { get => Vector3.Zero; set { } }
        public virtual Quaternion GizmoOrientation => Quaternion.Identity;
        public virtual void ApplyTranslateDelta(Vector3 worldDelta) { }
        public virtual void ApplyRotateDelta(Vector3 deltaEulerDegrees) { }
        public virtual void ApplyScaleFactor(float factor) { }
    }

    /// <summary>A collapsible, named section of the component tree (e.g. "Props (12)"), grouping
    /// one category's elements together instead of showing everything in one flat list.</summary>
    public class BmdCategoryGroupViewModel(string header, IReadOnlyCollection<BmdElementViewModel> items)
    {
        public string Header { get; } = header;
        public IReadOnlyCollection<BmdElementViewModel> Items { get; } = items;
    }

    // View models for specific element types
    public class BattlefieldBuildingViewModel(BattlefieldBuilding building) : BmdElementViewModel("Battlefield Building", building.BuildingKey, $"Version: {building.Version}")
    {
        public BattlefieldBuilding Building { get; } = building;
    }

    public class PropInfoViewModel(PropInfo prop, string propFilePath) : BmdElementViewModel(prop.IsDecal ? "Decal" : "Prop", System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop", $"Version: {prop.PropInfoVersion}")
    {
        public PropInfo Prop { get; } = prop;
        public string PropName { get; } = System.IO.Path.GetFileNameWithoutExtension(propFilePath) ?? "Unknown Prop";
        public string PropFilePath { get; } = propFilePath;

        // Full position/rotation/scale - props (including decals) store a real transform matrix.
        public float PositionX { get => Prop.Transform.Translation.X; set => SetPosition(value, PositionY, PositionZ); }
        public float PositionY { get => Prop.Transform.Translation.Y; set => SetPosition(PositionX, value, PositionZ); }
        public float PositionZ { get => Prop.Transform.Translation.Z; set => SetPosition(PositionX, PositionY, value); }

        public float RotationXDegrees { get => TerryTransform.Decompose(Prop.Transform).EulerDegrees.X; set => SetRotation(value, RotationYDegrees, RotationZDegrees); }
        public float RotationYDegrees { get => TerryTransform.Decompose(Prop.Transform).EulerDegrees.Y; set => SetRotation(RotationXDegrees, value, RotationZDegrees); }
        public float RotationZDegrees { get => TerryTransform.Decompose(Prop.Transform).EulerDegrees.Z; set => SetRotation(RotationXDegrees, RotationYDegrees, value); }

        public float ScaleX { get => TerryTransform.Decompose(Prop.Transform).Scale.X; set => SetScale(value, ScaleY, ScaleZ); }
        public float ScaleY { get => TerryTransform.Decompose(Prop.Transform).Scale.Y; set => SetScale(ScaleX, value, ScaleZ); }
        public float ScaleZ { get => TerryTransform.Decompose(Prop.Transform).Scale.Z; set => SetScale(ScaleX, ScaleY, value); }

        void SetPosition(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Prop.Transform);
            Prop.Transform = TerryTransform.Compose(new Vector3(x, y, z), d.EulerDegrees, d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetRotation(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Prop.Transform);
            Prop.Transform = TerryTransform.Compose(d.Position, new Vector3(x, y, z), d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetScale(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Prop.Transform);
            Prop.Transform = TerryTransform.Compose(d.Position, d.EulerDegrees, new Vector3(x, y, z));
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override bool SupportsRotate => true;
        public override bool SupportsScale => true;
        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set => SetPosition(value.X, value.Y, value.Z); }
        public override Quaternion GizmoOrientation => TerryTransform.EulerDegreesToQuaternion(new Vector3(RotationXDegrees, RotationYDegrees, RotationZDegrees));
        public override void ApplyTranslateDelta(Vector3 worldDelta) => SetPosition(PositionX + worldDelta.X, PositionY + worldDelta.Y, PositionZ + worldDelta.Z);
        public override void ApplyRotateDelta(Vector3 deltaEulerDegrees) => SetRotation(RotationXDegrees + deltaEulerDegrees.X, RotationYDegrees + deltaEulerDegrees.Y, RotationZDegrees + deltaEulerDegrees.Z);
        public override void ApplyScaleFactor(float factor) => SetScale(ScaleX * factor, ScaleY * factor, ScaleZ * factor);
    }

    public class VfxInfoViewModel(VfxInfo vfx) : BmdElementViewModel("VFX", vfx.VfxString, $"Version: {vfx.VfxInfoVersion}")
    {
        public VfxInfo Vfx { get; } = vfx;

        // Full position/rotation/scale - VFX carries a real transform matrix.
        public float PositionX { get => Vfx.Transform.Translation.X; set => SetPosition(value, PositionY, PositionZ); }
        public float PositionY { get => Vfx.Transform.Translation.Y; set => SetPosition(PositionX, value, PositionZ); }
        public float PositionZ { get => Vfx.Transform.Translation.Z; set => SetPosition(PositionX, PositionY, value); }

        public float RotationXDegrees { get => TerryTransform.Decompose(Vfx.Transform).EulerDegrees.X; set => SetRotation(value, RotationYDegrees, RotationZDegrees); }
        public float RotationYDegrees { get => TerryTransform.Decompose(Vfx.Transform).EulerDegrees.Y; set => SetRotation(RotationXDegrees, value, RotationZDegrees); }
        public float RotationZDegrees { get => TerryTransform.Decompose(Vfx.Transform).EulerDegrees.Z; set => SetRotation(RotationXDegrees, RotationYDegrees, value); }

        public float ScaleX { get => TerryTransform.Decompose(Vfx.Transform).Scale.X; set => SetScale(value, ScaleY, ScaleZ); }
        public float ScaleY { get => TerryTransform.Decompose(Vfx.Transform).Scale.Y; set => SetScale(ScaleX, value, ScaleZ); }
        public float ScaleZ { get => TerryTransform.Decompose(Vfx.Transform).Scale.Z; set => SetScale(ScaleX, ScaleY, value); }

        void SetPosition(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Vfx.Transform);
            Vfx.Transform = TerryTransform.Compose(new Vector3(x, y, z), d.EulerDegrees, d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetRotation(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Vfx.Transform);
            Vfx.Transform = TerryTransform.Compose(d.Position, new Vector3(x, y, z), d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetScale(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Vfx.Transform);
            Vfx.Transform = TerryTransform.Compose(d.Position, d.EulerDegrees, new Vector3(x, y, z));
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override bool SupportsRotate => true;
        public override bool SupportsScale => true;
        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set => SetPosition(value.X, value.Y, value.Z); }
        public override Quaternion GizmoOrientation => TerryTransform.EulerDegreesToQuaternion(new Vector3(RotationXDegrees, RotationYDegrees, RotationZDegrees));
        public override void ApplyTranslateDelta(Vector3 worldDelta) => SetPosition(PositionX + worldDelta.X, PositionY + worldDelta.Y, PositionZ + worldDelta.Z);
        public override void ApplyRotateDelta(Vector3 deltaEulerDegrees) => SetRotation(RotationXDegrees + deltaEulerDegrees.X, RotationYDegrees + deltaEulerDegrees.Y, RotationZDegrees + deltaEulerDegrees.Z);
        public override void ApplyScaleFactor(float factor) => SetScale(ScaleX * factor, ScaleY * factor, ScaleZ * factor);
    }

    public class PointLightInfoViewModel(PointLightInfo light) : BmdElementViewModel("Point Light", $"Point Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})",
          $"Radius: {light.Radius:F1}, Color: ({light.Red:F1}, {light.Green:F1}, {light.Blue:F1})")
    {
        public PointLightInfo Light { get; } = light;

        // Position only - a point light is omnidirectional, no rotation/scale in the format.
        public float PositionX { get => Light.Position.X; set { Light.Position = new RmvVector3(value, Light.Position.Y, Light.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionY { get => Light.Position.Y; set { Light.Position = new RmvVector3(Light.Position.X, value, Light.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionZ { get => Light.Position.Z; set { Light.Position = new RmvVector3(Light.Position.X, Light.Position.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; } }
        public override void ApplyTranslateDelta(Vector3 worldDelta) => GizmoPosition += worldDelta;

        /// <summary>Names match <c>BmdTerryProjectWriter.BuildPointLightEntity</c>'s mapping.</summary>
        public string[] AnimationTypeNames { get; } = ["LAT_NONE", "LAT_RADIUS_SIN", "LAT_RADIUS_SIN_SIN"];

        public string AnimationTypeName
        {
            get => Light.AnimationTypeEnum switch { 1 => "LAT_RADIUS_SIN", 2 => "LAT_RADIUS_SIN_SIN", _ => "LAT_NONE" };
            set
            {
                Light.AnimationTypeEnum = value switch { "LAT_RADIUS_SIN" => 1, "LAT_RADIUS_SIN_SIN" => 2, _ => (byte)0 };
                NotifyPropertyChanged();
                RaiseModified();
            }
        }
    }

    public class SpotLightInfoViewModel(SpotLightInfo light) : BmdElementViewModel("Spot Light", $"Spot Light at ({light.Position.X:F1}, {light.Position.Y:F1}, {light.Position.Z:F1})",
          $"RGB: ({light.IntensityRed:F2},{light.IntensityGreen:F2},{light.IntensityBlue:F2}), Length: {light.Length:F2}")
    {
        public SpotLightInfo Light { get; } = light;

        // Position + rotation (from the stored quaternion) - no scale field in the format.
        public float PositionX { get => Light.Position.X; set { Light.Position = new RmvVector3(value, Light.Position.Y, Light.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionY { get => Light.Position.Y; set { Light.Position = new RmvVector3(Light.Position.X, value, Light.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionZ { get => Light.Position.Z; set { Light.Position = new RmvVector3(Light.Position.X, Light.Position.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        Vector3 EulerDegrees => TerryTransform.QuaternionToEulerDegrees(Light.QuartX, Light.QuartY, Light.QuartZ, Light.QuartW);

        public float RotationXDegrees { get => EulerDegrees.X; set => SetRotation(value, RotationYDegrees, RotationZDegrees); }
        public float RotationYDegrees { get => EulerDegrees.Y; set => SetRotation(RotationXDegrees, value, RotationZDegrees); }
        public float RotationZDegrees { get => EulerDegrees.Z; set => SetRotation(RotationXDegrees, RotationYDegrees, value); }

        void SetRotation(float x, float y, float z)
        {
            var q = TerryTransform.EulerDegreesToQuaternion(new Vector3(x, y, z));
            Light.QuartX = q.X;
            Light.QuartY = q.Y;
            Light.QuartZ = q.Z;
            Light.QuartW = q.W;
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override bool SupportsRotate => true;
        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; } }
        public override Quaternion GizmoOrientation => new(Light.QuartX, Light.QuartY, Light.QuartZ, Light.QuartW);
        public override void ApplyTranslateDelta(Vector3 worldDelta) => GizmoPosition += worldDelta;
        public override void ApplyRotateDelta(Vector3 deltaEulerDegrees) => SetRotation(RotationXDegrees + deltaEulerDegrees.X, RotationYDegrees + deltaEulerDegrees.Y, RotationZDegrees + deltaEulerDegrees.Z);
    }

    public class SoundInfoViewModel(SoundInfo sound) : BmdElementViewModel("Sound", sound.SoundString, $"Type: {sound.TypeString}, Version: {sound.Version}")
    {
        public SoundInfo Sound { get; } = sound;

        // Position only, per-coordinate - sounds have no rotation/scale, and multi-point sounds
        // (SST_LINE_LIST/SST_MULTI_POINT) need each coordinate independently movable. CoordList[0]
        // also doubles as "the" position for single-point sounds (SST_SPHERE) and as the gizmo's
        // drag target (OffsetAll moves every point together, rigidly).
        public ObservableCollection<SoundPointViewModel> Points { get; } =
            new(System.Linq.Enumerable.Range(0, sound.CoordList.Length).Select(i => new SoundPointViewModel(sound, i)));

        public bool HasPoints => Sound.CoordList.Length > 0;

        public float PositionX { get => HasPoints ? Sound.CoordList[0].X : 0; set { if (HasPoints) { Sound.CoordList[0].X = value; NotifyPropertyChanged(); RaiseModified(); } } }
        public float PositionY { get => HasPoints ? Sound.CoordList[0].Y : 0; set { if (HasPoints) { Sound.CoordList[0].Y = value; NotifyPropertyChanged(); RaiseModified(); } } }
        public float PositionZ { get => HasPoints ? Sound.CoordList[0].Z : 0; set { if (HasPoints) { Sound.CoordList[0].Z = value; NotifyPropertyChanged(); RaiseModified(); } } }

        /// <summary>Rigidly offsets every coordinate by the same delta - used by the gizmo drag.</summary>
        public void OffsetAll(Vector3 delta)
        {
            for (var i = 0; i < Sound.CoordList.Length; i++)
            {
                Sound.CoordList[i].X += delta.X;
                Sound.CoordList[i].Y += delta.Y;
                Sound.CoordList[i].Z += delta.Z;
            }
            foreach (var p in Points)
                p.RefreshFromDomain();
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set => OffsetAll(value - GizmoPosition); }
        public override void ApplyTranslateDelta(Vector3 worldDelta) => OffsetAll(worldDelta);

        /// <summary>Known values seen across the corpus and referenced by the Terry exporter; the
        /// combo box is editable too since this field isn't a strictly closed enum.</summary>
        public string[] TypeNames { get; } = ["SST_POINT", "SST_SPHERE", "SST_LINE_LIST", "SST_MULTI_POINT", "SST_RIVER"];
    }

    /// <summary>One editable coordinate within a <see cref="SoundInfoViewModel"/>'s CoordList.</summary>
    public class SoundPointViewModel(SoundInfo sound, int index) : NotifyPropertyChangedImpl
    {
        public int Index => index;
        public float X { get => sound.CoordList[index].X; set { sound.CoordList[index].X = value; NotifyPropertyChanged(); } }
        public float Y { get => sound.CoordList[index].Y; set { sound.CoordList[index].Y = value; NotifyPropertyChanged(); } }
        public float Z { get => sound.CoordList[index].Z; set { sound.CoordList[index].Z = value; NotifyPropertyChanged(); } }
        public void RefreshFromDomain() => NotifyPropertyChanged(string.Empty);
    }

    public class PolyMeshInfoViewModel(PolyMeshInfo mesh) : BmdElementViewModel("PolyMesh", mesh.MaterialString, $"Vertices: {mesh.VertexList.Length}, Triangles: {mesh.TriangleList.Length / 3}")
    {
        public PolyMeshInfo Mesh { get; } = mesh;

        /// <summary>Version &gt; 3 carries a real transform matrix (full TRS); version &le; 3 has
        /// no separate transform at all - its vertices are already baked into world space, so
        /// "moving" it means bulk-offsetting every vertex (translate only, no rotate/scale).</summary>
        public bool HasTransform => Mesh.PolyMeshVersion > 3;

        public float PositionX
        {
            get => HasTransform ? Mesh.Transform.Translation.X : (Mesh.VertexList.Length > 0 ? Mesh.VertexList[0].X : 0);
            set { if (HasTransform) SetPosition(value, PositionY, PositionZ); else OffsetVertices(value - PositionX, 0, 0); }
        }
        public float PositionY
        {
            get => HasTransform ? Mesh.Transform.Translation.Y : (Mesh.VertexList.Length > 0 ? Mesh.VertexList[0].Y : 0);
            set { if (HasTransform) SetPosition(PositionX, value, PositionZ); else OffsetVertices(0, value - PositionY, 0); }
        }
        public float PositionZ
        {
            get => HasTransform ? Mesh.Transform.Translation.Z : (Mesh.VertexList.Length > 0 ? Mesh.VertexList[0].Z : 0);
            set { if (HasTransform) SetPosition(PositionX, PositionY, value); else OffsetVertices(0, 0, value - PositionZ); }
        }

        public float RotationXDegrees { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).EulerDegrees.X : 0; set { if (HasTransform) SetRotation(value, RotationYDegrees, RotationZDegrees); } }
        public float RotationYDegrees { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).EulerDegrees.Y : 0; set { if (HasTransform) SetRotation(RotationXDegrees, value, RotationZDegrees); } }
        public float RotationZDegrees { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).EulerDegrees.Z : 0; set { if (HasTransform) SetRotation(RotationXDegrees, RotationYDegrees, value); } }

        public float ScaleX { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).Scale.X : 1; set { if (HasTransform) SetScale(value, ScaleY, ScaleZ); } }
        public float ScaleY { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).Scale.Y : 1; set { if (HasTransform) SetScale(ScaleX, value, ScaleZ); } }
        public float ScaleZ { get => HasTransform ? TerryTransform.Decompose(Mesh.Transform).Scale.Z : 1; set { if (HasTransform) SetScale(ScaleX, ScaleY, value); } }

        void SetPosition(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Mesh.Transform);
            Mesh.Transform = TerryTransform.Compose(new Vector3(x, y, z), d.EulerDegrees, d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetRotation(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Mesh.Transform);
            Mesh.Transform = TerryTransform.Compose(d.Position, new Vector3(x, y, z), d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetScale(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Mesh.Transform);
            Mesh.Transform = TerryTransform.Compose(d.Position, d.EulerDegrees, new Vector3(x, y, z));
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void OffsetVertices(float dx, float dy, float dz)
        {
            for (var i = 0; i < Mesh.VertexList.Length; i++)
            {
                Mesh.VertexList[i].X += dx;
                Mesh.VertexList[i].Y += dy;
                Mesh.VertexList[i].Z += dz;
            }
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override bool SupportsRotate => HasTransform;
        public override bool SupportsScale => HasTransform;
        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; } }
        public override Quaternion GizmoOrientation => HasTransform ? TerryTransform.EulerDegreesToQuaternion(new Vector3(RotationXDegrees, RotationYDegrees, RotationZDegrees)) : Quaternion.Identity;
        public override void ApplyTranslateDelta(Vector3 worldDelta) { if (HasTransform) SetPosition(PositionX + worldDelta.X, PositionY + worldDelta.Y, PositionZ + worldDelta.Z); else OffsetVertices(worldDelta.X, worldDelta.Y, worldDelta.Z); }
        public override void ApplyRotateDelta(Vector3 deltaEulerDegrees) { if (HasTransform) SetRotation(RotationXDegrees + deltaEulerDegrees.X, RotationYDegrees + deltaEulerDegrees.Y, RotationZDegrees + deltaEulerDegrees.Z); }
        public override void ApplyScaleFactor(float factor) { if (HasTransform) SetScale(ScaleX * factor, ScaleY * factor, ScaleZ * factor); }
    }

    public class LightProbeInfoViewModel(LightProbeInfo probe) : BmdElementViewModel("Light Probe", $"Probe_{probe.Position.X:F2}_{probe.Position.Y:F2}_{probe.Position.Z:F2}",
          $"Inner: {probe.InnerRadius:F2}, Outer: {probe.OuterRadius:F2}, Primary: {probe.Primary}")
    {
        public LightProbeInfo Probe { get; } = probe;

        // Position only - light probes have no rotation, and radius (not "scale") controls extent.
        public float PositionX { get => Probe.Position.X; set { Probe.Position = new RmvVector3(value, Probe.Position.Y, Probe.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionY { get => Probe.Position.Y; set { Probe.Position = new RmvVector3(Probe.Position.X, value, Probe.Position.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float PositionZ { get => Probe.Position.Z; set { Probe.Position = new RmvVector3(Probe.Position.X, Probe.Position.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; } }
        public override void ApplyTranslateDelta(Vector3 worldDelta) => GizmoPosition += worldDelta;
    }

    public class TerrainHoleInfoViewModel(TerrainHoleTriangleInfo hole) : BmdElementViewModel("Terrain Hole", $"Hole at ({hole.FirstVert.X:F1}, {hole.FirstVert.Y:F1}, {hole.FirstVert.Z:F1})",
          $"Version: {hole.TerrainHoleVersion}")
    {
        public TerrainHoleTriangleInfo Hole { get; } = hole;

        // Three independently draggable vertices - a triangle has no single "transform", and no
        // rotation/scale concept applies to a set of raw points.
        public float FirstVertX { get => Hole.FirstVert.X; set { Hole.FirstVert = new RmvVector3(value, Hole.FirstVert.Y, Hole.FirstVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float FirstVertY { get => Hole.FirstVert.Y; set { Hole.FirstVert = new RmvVector3(Hole.FirstVert.X, value, Hole.FirstVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float FirstVertZ { get => Hole.FirstVert.Z; set { Hole.FirstVert = new RmvVector3(Hole.FirstVert.X, Hole.FirstVert.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        public float SecondVertX { get => Hole.SecondVert.X; set { Hole.SecondVert = new RmvVector3(value, Hole.SecondVert.Y, Hole.SecondVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float SecondVertY { get => Hole.SecondVert.Y; set { Hole.SecondVert = new RmvVector3(Hole.SecondVert.X, value, Hole.SecondVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float SecondVertZ { get => Hole.SecondVert.Z; set { Hole.SecondVert = new RmvVector3(Hole.SecondVert.X, Hole.SecondVert.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        public float ThirdVertX { get => Hole.ThirdVert.X; set { Hole.ThirdVert = new RmvVector3(value, Hole.ThirdVert.Y, Hole.ThirdVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float ThirdVertY { get => Hole.ThirdVert.Y; set { Hole.ThirdVert = new RmvVector3(Hole.ThirdVert.X, value, Hole.ThirdVert.Z); NotifyPropertyChanged(); RaiseModified(); } }
        public float ThirdVertZ { get => Hole.ThirdVert.Z; set { Hole.ThirdVert = new RmvVector3(Hole.ThirdVert.X, Hole.ThirdVert.Y, value); NotifyPropertyChanged(); RaiseModified(); } }

        /// <summary>Rigidly offsets all three vertices by the same delta - used by the gizmo drag.</summary>
        public void OffsetAll(Vector3 delta)
        {
            Hole.FirstVert = new RmvVector3(Hole.FirstVert.X + delta.X, Hole.FirstVert.Y + delta.Y, Hole.FirstVert.Z + delta.Z);
            Hole.SecondVert = new RmvVector3(Hole.SecondVert.X + delta.X, Hole.SecondVert.Y + delta.Y, Hole.SecondVert.Z + delta.Z);
            Hole.ThirdVert = new RmvVector3(Hole.ThirdVert.X + delta.X, Hole.ThirdVert.Y + delta.Y, Hole.ThirdVert.Z + delta.Z);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override Vector3 GizmoPosition { get => new(FirstVertX, FirstVertY, FirstVertZ); set => OffsetAll(value - GizmoPosition); }
        public override void ApplyTranslateDelta(Vector3 worldDelta) => OffsetAll(worldDelta);
    }

    public class CscInfoViewModel(CscInfo csc) : BmdElementViewModel("CSC Info", csc.SceneFile, $"Version: {csc.Version}")
    {
        public CscInfo Csc { get; } = csc;

        // Full position/rotation/scale - composite scene references carry a real transform matrix.
        public float PositionX { get => Csc.Transform.Translation.X; set => SetPosition(value, PositionY, PositionZ); }
        public float PositionY { get => Csc.Transform.Translation.Y; set => SetPosition(PositionX, value, PositionZ); }
        public float PositionZ { get => Csc.Transform.Translation.Z; set => SetPosition(PositionX, PositionY, value); }

        public float RotationXDegrees { get => TerryTransform.Decompose(Csc.Transform).EulerDegrees.X; set => SetRotation(value, RotationYDegrees, RotationZDegrees); }
        public float RotationYDegrees { get => TerryTransform.Decompose(Csc.Transform).EulerDegrees.Y; set => SetRotation(RotationXDegrees, value, RotationZDegrees); }
        public float RotationZDegrees { get => TerryTransform.Decompose(Csc.Transform).EulerDegrees.Z; set => SetRotation(RotationXDegrees, RotationYDegrees, value); }

        public float ScaleX { get => TerryTransform.Decompose(Csc.Transform).Scale.X; set => SetScale(value, ScaleY, ScaleZ); }
        public float ScaleY { get => TerryTransform.Decompose(Csc.Transform).Scale.Y; set => SetScale(ScaleX, value, ScaleZ); }
        public float ScaleZ { get => TerryTransform.Decompose(Csc.Transform).Scale.Z; set => SetScale(ScaleX, ScaleY, value); }

        void SetPosition(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Csc.Transform);
            Csc.Transform = TerryTransform.Compose(new Vector3(x, y, z), d.EulerDegrees, d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetRotation(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Csc.Transform);
            Csc.Transform = TerryTransform.Compose(d.Position, new Vector3(x, y, z), d.Scale);
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        void SetScale(float x, float y, float z)
        {
            var d = TerryTransform.Decompose(Csc.Transform);
            Csc.Transform = TerryTransform.Compose(d.Position, d.EulerDegrees, new Vector3(x, y, z));
            NotifyPropertyChanged(string.Empty);
            RaiseModified();
        }

        public override bool SupportsRotate => true;
        public override bool SupportsScale => true;
        public override Vector3 GizmoPosition { get => new(PositionX, PositionY, PositionZ); set => SetPosition(value.X, value.Y, value.Z); }
        public override Quaternion GizmoOrientation => TerryTransform.EulerDegreesToQuaternion(new Vector3(RotationXDegrees, RotationYDegrees, RotationZDegrees));
        public override void ApplyTranslateDelta(Vector3 worldDelta) => SetPosition(PositionX + worldDelta.X, PositionY + worldDelta.Y, PositionZ + worldDelta.Z);
        public override void ApplyRotateDelta(Vector3 deltaEulerDegrees) => SetRotation(RotationXDegrees + deltaEulerDegrees.X, RotationYDegrees + deltaEulerDegrees.Y, RotationZDegrees + deltaEulerDegrees.Z);
        public override void ApplyScaleFactor(float factor) => SetScale(ScaleX * factor, ScaleY * factor, ScaleZ * factor);
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
