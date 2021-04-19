using Common;
using Common.ApplicationSettings;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Windows;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Scene;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel
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

        ModelLoaderService _modelLoader;
        ModelSaverHelper _modelSaver;

        public KitbasherViewModel(PackFileService pf, SkeletonAnimationLookUpHelper skeletonHelper)
        {
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = skeletonHelper;
            //
            Scene = new SceneContainer();
            //
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
            
            Animation = new AnimationControllerViewModel(Scene, _packFileService, _skeletonAnimationLookUpHelper);
            
            SceneExplorer = new SceneExplorerViewModel(Scene, _skeletonAnimationLookUpHelper, _packFileService, Animation);
            Scene.Components.Add(SceneExplorer);
            
            MenuBar = new MenuBarViewModel(Scene, _packFileService);
            
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
                    _modelLoader.LoadEditableModel(MainFile as PackFile);
                    DisplayName = MainFile.Name;
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Error loading file {MainFile?.Name} - {e}");
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
    }
}
