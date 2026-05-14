using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Core.Services;
using Shared.GameFormats.Bmd;
using Shared.GameFormats.RigidModel;
using GameWorld.Core.Components;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Services;
using GameWorld.Core.SceneNodes;
using Editors.BmdEditor.Services;
using Serilog;

namespace Editors.BmdEditor.ViewModels
{
    public partial class BmdSceneViewModel : ObservableObject, IDisposable
    {
        private readonly ILogger _logger = Serilog.Log.ForContext<BmdSceneViewModel>();
        private readonly IPackFileService _packFileService;
        private readonly IEditorManager _editorCreator;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;
        private readonly MeshBuilderService _meshBuilderService;
        private readonly CapabilityMaterialFactory _materialFactory;
        private readonly BmdSceneCreator _bmdSceneCreator;

        private BmdFile? _bmdFile;
        private IWpfGame? _scene3D;

        [ObservableProperty] string _statusText = "Ready";
        [ObservableProperty] string _sceneInfo = "No scene loaded";
        [ObservableProperty] int _loadedPropsCount = 0;
        [ObservableProperty] bool _showGrid = true;
        [ObservableProperty] bool _showProps = true;
        [ObservableProperty] bool _showLights = true;

        [ObservableProperty] string _displayName = "BMD 3D Scene";

        public IWpfGame? Scene3D
        {
            get => _scene3D;
            private set => SetProperty(ref _scene3D, value);
        }

        public ICommand ResetCameraCommand { get; }
        public ICommand ToggleGridCommand { get; }
        public ICommand TogglePropsCommand { get; }
        public ICommand ToggleLightsCommand { get; }

        public BmdSceneViewModel(
            IPackFileService packFileService,
            IEditorManager editorCreator,
            ResourceLibrary resourceLibrary,
            IGraphicsResourceCreator graphicsResourceCreator,
            MeshBuilderService meshBuilderService,
            CapabilityMaterialFactory materialFactory,
            GameWorld.Core.Components.SceneManager sceneManager,
            GameWorld.Core.SceneNodes.Rmv2ModelNodeLoader rmv2ModelNodeLoader)
        {
            _packFileService = packFileService;
            _editorCreator = editorCreator;
            _resourceLibrary = resourceLibrary;
            _graphicsResourceCreator = graphicsResourceCreator;
            _meshBuilderService = meshBuilderService;
            _materialFactory = materialFactory;
            _bmdSceneCreator = new BmdSceneCreator(packFileService, sceneManager, rmv2ModelNodeLoader, resourceLibrary, meshBuilderService);

            ResetCameraCommand = new RelayCommand(ResetCamera);
            ToggleGridCommand = new RelayCommand(ToggleGrid);
            TogglePropsCommand = new RelayCommand(ToggleProps);
            ToggleLightsCommand = new RelayCommand(ToggleLights);
        }

        public void LoadBmdFile(BmdFile bmdFile, PackFile packFile)
        {
            _bmdFile = bmdFile;
            DisplayName = packFile.Name;

            try
            {
                _logger.Information($"Loading BMD file into 3D scene: {packFile.Name}");
                StatusText = "Loading 3D scene...";

                // Use the BmdSceneCreator to create the scene
                _bmdSceneCreator.CreateSceneFromBmd(bmdFile, packFile);

                // Update counts and info
                LoadedPropsCount = Math.Min(bmdFile.Props.Count, bmdFile.PropInfos.Count);
                UpdateSceneInfo();
                
                StatusText = "Scene loaded successfully";
                _logger.Information($"BMD 3D scene loaded successfully with {LoadedPropsCount} props");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load BMD file into 3D scene: {ex.Message}");
                StatusText = "Failed to load scene";
                throw;
            }
        }

        
        private void UpdateSceneInfo()
        {
            var info = $"Props: {LoadedPropsCount}";
            if (_bmdFile != null)
            {
                info += $" | Lights: {_bmdFile.PointLights.Count + _bmdFile.SpotLights.Count}";
                info += $" | VFX: {_bmdFile.VfxInfos.Count}";
            }
            SceneInfo = info;
        }

        private void ResetCamera()
        {
            // TODO: Reset camera to default position
            _logger.Information("Camera reset");
        }

        private void ToggleGrid()
        {
            ShowGrid = !ShowGrid;
            // TODO: Show/hide grid in scene
            _logger.Information($"Grid visibility: {ShowGrid}");
        }

        private void ToggleProps()
        {
            ShowProps = !ShowProps;
            // TODO: Show/hide props in scene
            _logger.Information($"Props visibility: {ShowProps}");
        }

        private void ToggleLights()
        {
            ShowLights = !ShowLights;
            // TODO: Show/hide lights in scene
            _logger.Information($"Lights visibility: {ShowLights}");
        }

        public void Dispose()
        {
            // Cleanup resources
            Scene3D = null;
            _bmdFile = null;
        }
    }
}
