using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Commands;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.Services;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        ILogger _logger = Logging.Create<KitbasherViewModel>();

        public SceneContainer Scene { get; set; }
        public SceneExplorerViewModel SceneExplorer { get; set; }

        string _displayName = "3d viewer";
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }


        IPackFile _packFile;
        public IPackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }

        PackFileService _packFileService;

        public KitbasherViewModel(PackFileService pf)
        {
            _packFileService = pf;
            Scene = new SceneContainer();

            Scene.Components.Add(new FpsComponent(Scene));
            Scene.Components.Add(new KeyboardComponent(Scene));
            Scene.Components.Add(new MouseComponent(Scene));
            Scene.Components.Add(new ResourceLibary(Scene));
            Scene.Components.Add(new ArcBallCamera(Scene, new Vector3(0), 10));
            Scene.Components.Add(new SceneManager(Scene));
            Scene.Components.Add(new SelectionManager(Scene));
            Scene.Components.Add(new CommandExecutor(Scene));
            Scene.Components.Add(new GizmoComponent(Scene));
            Scene.Components.Add(new SelectionComponent(Scene));
            Scene.Components.Add(new ObjectEditor(Scene));
            Scene.Components.Add(new FaceEditor(Scene));
            Scene.Components.Add(new FocusSelectableObjectComponent(Scene));

            SceneExplorer = new SceneExplorerViewModel(Scene);

            Scene.SceneInitialized += OnSceneInitialized;
        }


        Rmv2ModelNode _editableRmvMesh;
        SceneNode _referenceMesh;

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();
            
            _editableRmvMesh = (Rmv2ModelNode)sceneManager.RootNode.AddObject(new Rmv2ModelNode("Editable Model"));
            _referenceMesh = sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false});
           
            if (MainFile != null)
            {
                var file = MainFile as PackFile;
                _editableRmvMesh.AddModel(new RmvRigidModel(file.DataSource.ReadData(), file.FullPath), Scene.GraphicsDevice);

                var cubeMesh = new CubeMesh(Scene.GraphicsDevice);
                _editableRmvMesh.Children[0].AddObject(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(0, 0, 0), new Vector3(0.5f), "Item1", Scene.GraphicsDevice));
            }

            // Wmd reference
            var refereneceMesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");
            SceneLoader loader = new SceneLoader(_packFileService, Scene.GraphicsDevice);
            var result = loader.Load(refereneceMesh as PackFile, null);
            loader.Simplify(result);
            result.ForeachNode((node) => node.IsEditable = false);
            _referenceMesh.AddObject(result);



            SceneExplorer.EditableMeshNode = _editableRmvMesh;
        }

        public void AddMesh(IPackFile file, bool isReference)
        {
            if (file is PackFile packFile)
            {
                var data = packFile.DataSource.ReadData();
            }
        }

        public string Text { get; set; }

        public bool Save()
        {
            throw new NotImplementedException();
        }
        void SetCurrentPackFile(IPackFile packedFile)
        {


        }
    }
}
