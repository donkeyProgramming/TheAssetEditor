using Common;
using Common.ApplicationSettings;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using g3;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{

    public class ModelLoader
    {
        ILogger _logger = Logging.Create<ModelLoader>();

        public Rmv2ModelNode EditableMeshNode { get; private set; }
        public ISceneNode ReferenceMeshRoot { get; private set; }

        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        AnimationControllerViewModel _animationView;
        SceneManager _sceneManager;

        public ModelLoader(PackFileService packFileService, ResourceLibary resourceLibary, AnimationControllerViewModel animationView, SceneManager sceneManager)
        {
            _packFileService = packFileService;
            _resourceLibary = resourceLibary;
            _animationView = animationView;
            _sceneManager = sceneManager;

            _sceneManager.RootNode.AddObject(new SkeletonNode(resourceLibary.Content, animationView));
            EditableMeshNode = (Rmv2ModelNode)_sceneManager.RootNode.AddObject(new Rmv2ModelNode("Editable Model"));
            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                lodNode.IsVisible = lodIndex == 0;
                EditableMeshNode.AddObject(lodNode);
            }

            ReferenceMeshRoot = sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false });
        }

        public void LoadEditableModel(PackFile file)
        {

            var rmv = new RmvRigidModel(file.DataSource.ReadData(), file.Name);
            EditableMeshNode.SetModel(rmv, _resourceLibary.GraphicsDevice, _resourceLibary, _animationView.Player);

            _animationView.SetActiveSkeleton(rmv.Header.SkeletonName);
        }

        public void LoadReference(string path)
        {
            _logger.Here().Information($"Loading reference model from path - {path}");

            var refereneceMesh = _packFileService.FindFile(path);
            if (refereneceMesh == null)
            {
                _logger.Here().Error("Unable to find file");
                return;
            }

       
           
            LoadReference(refereneceMesh as PackFile);
        }

        public void LoadReference(PackFile file)
        {
            _logger.Here().Information($"Loading reference model - {_packFileService.GetFullPath(file)}");

            SceneLoader loader = new SceneLoader(_packFileService, _resourceLibary);
            var result = loader.Load(file, null, _animationView.Player);
            if (result == null)
            {
                _logger.Here().Error("Unable to load model");
                return;
            }

            result.ForeachNode((node) => 
            { 
                node.IsEditable = false;
                if (node is ISelectable selectable)
                    selectable.IsSelectable = false;
            });
            ReferenceMeshRoot.AddObject(result);
        }
    }

    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public SceneContainer Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; } 
        public AnimationControllerViewModel Animation { get; set; }


        string _displayName = "3d viewer";
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        public IPackFile MainFile { get; set; }

        ModelLoader _modelLoader;

        public KitbasherViewModel(PackFileService pf)
        {
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = new SkeletonAnimationLookUpHelper();
            _skeletonAnimationLookUpHelper.Initialize(_packFileService);

            Scene = new SceneContainer();
            
            Scene.Components.Add(new FpsComponent(Scene));
            Scene.Components.Add(new KeyboardComponent(Scene));
            Scene.Components.Add(new MouseComponent(Scene));
            Scene.Components.Add(new ResourceLibary(Scene, pf));
            Scene.Components.Add(new ArcBallCamera(Scene, new Vector3(0), 10));
            Scene.Components.Add(new SceneManager(Scene));
            Scene.Components.Add(new SelectionManager(Scene));
            Scene.Components.Add(new CommandExecutor(Scene));
            Scene.Components.Add(new GizmoComponent(Scene));
            Scene.Components.Add(new SelectionComponent(Scene));
            Scene.Components.Add(new ObjectEditor(Scene));
            Scene.Components.Add(new FaceEditor(Scene));
            Scene.Components.Add(new FocusSelectableObjectComponent(Scene));
            Scene.Components.Add(new ClearScreenComponent(Scene));
            Scene.Components.Add(new RenderEngineComponent(Scene));
            Scene.Components.Add(new GridComponent(Scene));
            Scene.Components.Add(new AnimationsContainerComponent(Scene)); 


            SceneExplorer = new SceneExplorerViewModel(Scene, _skeletonAnimationLookUpHelper);
            Scene.Components.Add(SceneExplorer);

            MenuBar = new MenuBarViewModel(Scene, _packFileService);
            Animation = new AnimationControllerViewModel(Scene, _packFileService, _skeletonAnimationLookUpHelper);
            Scene.SceneInitialized += OnSceneInitialized;
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();
            var resourceLib = scene.GetComponent<ResourceLibary>();
            _modelLoader = new ModelLoader(_packFileService, resourceLib, Animation, sceneManager);
            MenuBar.ModelLoader = _modelLoader;

            SceneExplorer.EditableMeshNode = _modelLoader.EditableMeshNode;

            if (MainFile != null)
            {
                _modelLoader.LoadEditableModel(MainFile as PackFile);
                DisplayName = MainFile.Name;
            }

            // Add Wmd test reference
           
        }

        public bool Save()
        {
            throw new NotImplementedException();
        }
    }
}
