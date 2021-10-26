using Common;
using Common.ApplicationSettings;
using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using FileTypes.DB;
using FileTypes.PackFiles.Models;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IKitBashEditor, IDropTarget
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        PackFileService _packFileService;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        SceneContainer _scene;
        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; }
        public AnimationControllerViewModel Animation { get; set; }


        string _displayName = "3d viewer";
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        public IPackFile MainFile { get; set; }
        public IPackFile ReferenceModel { get; set; }

        ModelLoaderService _modelLoader;
        ModelSaverHelper _modelSaver;

        public KitbasherViewModel(PackFileService pf, SkeletonAnimationLookUpHelper skeletonHelper )
        {
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = skeletonHelper;
            //
            Scene = new SceneContainer();
            //
            Scene.AddCompnent(new FpsComponent(Scene));
            Scene.AddCompnent(new KeyboardComponent(Scene));
            Scene.AddCompnent(new MouseComponent(Scene));
            Scene.AddCompnent(new ResourceLibary(Scene, pf));
            Scene.AddCompnent(new ArcBallCamera(Scene));
            Scene.AddCompnent(new SceneManager(Scene));
            Scene.AddCompnent(new SelectionManager(Scene));
            Scene.AddCompnent(new CommandExecutor(Scene));
            Scene.AddCompnent(new GizmoComponent(Scene));
            Scene.AddCompnent(new SelectionComponent(Scene));
            Scene.AddCompnent(new ObjectEditor(Scene));
            Scene.AddCompnent(new FaceEditor(Scene));
            Scene.AddCompnent(new FocusSelectableObjectComponent(Scene));
            Scene.AddCompnent(new ClearScreenComponent(Scene));
            Scene.AddCompnent(new RenderEngineComponent(Scene));
            Scene.AddCompnent(new GridComponent(Scene));
            Scene.AddCompnent(new AnimationsContainerComponent(Scene));
            Scene.AddCompnent(new ViewOnlySelectedComponent(Scene));
            Scene.AddCompnent(new LightControllerComponent(Scene));
            
            Animation = new AnimationControllerViewModel(Scene, _packFileService, _skeletonAnimationLookUpHelper);
            
            SceneExplorer = new SceneExplorerViewModel(Scene, _skeletonAnimationLookUpHelper, _packFileService, Animation);
            Scene.Components.Add(SceneExplorer);
            
            MenuBar = new MenuBarViewModel(Scene, _packFileService, skeletonHelper);
            
            Scene.SceneInitialized += OnSceneInitialized;
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();
            var resourceLib = scene.GetComponent<ResourceLibary>();
            _modelLoader = new ModelLoaderService(_packFileService, resourceLib, Animation, sceneManager, MainFile);
            _modelSaver = new ModelSaverHelper(_packFileService, sceneManager, this, _modelLoader.EditableMeshNode);
            MenuBar.ModelLoader = _modelLoader;
            MenuBar.General.ModelSaver = _modelSaver;
            
            SceneExplorer.EditableMeshNode = _modelLoader.EditableMeshNode;
            
            if (MainFile != null)
            {
                try
                {
                    _modelLoader.LoadMainEditableModel(MainFile as PackFile);
                    var nodes = _modelLoader.EditableMeshNode.GetMeshNodes(0)
                        .Select(x => x as ISelectable)
                        .Where(x => x != null)
                        .ToList();
                    Scene.GetComponent<FocusSelectableObjectComponent>().FocusObjects(nodes);
                   DisplayName = MainFile.Name;
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Error loading file {MainFile?.Name} - {e}");
                    MessageBox.Show("Unable to load file");
                }
            }

            if (ReferenceModel != null)
            {
                try
                {
                    _modelLoader.LoadReference(ReferenceModel as PackFile);
                    DisplayName = ReferenceModel.Name;
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Error loading file {ReferenceModel?.Name} - {e}");
                    MessageBox.Show("Unable to load file");
                }
            }
        }

        public bool Save()
        {
            return true;
        }

        public void Close()
        {
            Scene.Dispose();
            Scene.SceneInitialized -= OnSceneInitialized;
            _modelSaver = null;
            _modelLoader = null;
            MenuBar = null;
            Scene = null;
            _packFileService = null;
            _skeletonAnimationLookUpHelper = null;
            Scene = null;
            SceneExplorer = null;
            MenuBar = null;
            Animation = null;
            MainFile = null;
            _modelLoader = null;
            _modelSaver = null;
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }

        public bool AllowDrop(TreeNode node)
        {
            if (node != null && node.NodeType == NodeType.File)
            {
                var extention = Path.GetExtension(node.Name).ToLower();
                if (extention == ".rigid_model_v2" || extention == ".wsmodel" || extention == ".variantmeshdefinition")
                    return true;
            }
            return false;
        }

        public bool Drop(TreeNode node)
        {
            _modelLoader.LoadReference(node.Item as PackFile);
            return true;
        }
    }
}
