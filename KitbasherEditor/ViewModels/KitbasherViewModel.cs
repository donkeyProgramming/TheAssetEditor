using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();
        PackFileService _packFileService;

        public SceneContainer Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }
        public MenuBarViewModel MenuBar { get; set; } 
        public AnimationControllerViewModel Animation { get; set; }


        string _displayName = "3d viewer";
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        public IPackFile MainFile { get; set; }

        public KitbasherViewModel(PackFileService pf)
        {
            _packFileService = pf;

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

            SceneExplorer = new SceneExplorerViewModel(Scene);
            Scene.Components.Add(SceneExplorer as IEditableMeshResolver);

            MenuBar = new MenuBarViewModel(Scene);
            Animation = new AnimationControllerViewModel(Scene, _packFileService);
            Scene.SceneInitialized += OnSceneInitialized;
        }

        Rmv2ModelNode _editableRmvMesh;
        SceneNode _referenceMesh;

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();
            var resourceLib = scene.GetComponent<ResourceLibary>();

            sceneManager.RootNode.AddObject(new SkeletonNode("I own the skeletonRender"));
            _editableRmvMesh = (Rmv2ModelNode)sceneManager.RootNode.AddObject(new Rmv2ModelNode("Editable Model"));
            
            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                lodNode.IsVisible = lodIndex == 0;
                _editableRmvMesh.AddObject(lodNode);
            }

            _referenceMesh = sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false});
           
            if (MainFile != null)
            {
                var file = MainFile as PackFile;
                var rmv = new RmvRigidModel(file.DataSource.ReadData(), file.Name);
                _editableRmvMesh.AddModel(rmv, Scene.GraphicsDevice, resourceLib);
                Animation.SetActiveSkeleton(rmv.Header.SkeletonName);
                DisplayName = file.Name;

                //var cubeMesh = new CubeMesh(Scene.GraphicsDevice);
                //_editableRmvMesh.Children[0].AddObject(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(0, 0, 0), new Vector3(0.5f), "Item1", Scene.GraphicsDevice));
            }
            
            // Wmd reference
            var refereneceMesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");
            SceneLoader loader = new SceneLoader(_packFileService, Scene.GraphicsDevice, resourceLib);
            var result = loader.Load(refereneceMesh as PackFile, null);
            loader.Simplify(result);
            result.ForeachNode((node) => node.IsEditable = false);
            _referenceMesh.AddObject(result);

            SceneExplorer.EditableMeshNode = _editableRmvMesh;
        }

        public bool Save()
        {
            throw new NotImplementedException();
        }
    }
}
