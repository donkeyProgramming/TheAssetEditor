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

            SceneExplorer = new SceneExplorerViewModel(Scene);

            Scene.SceneInitialized += OnSceneInitialized;
        }


        SceneNode _editableMesh;
        SceneNode _referenceMesh;

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();


            _editableMesh = sceneManager.RootNode.AddObject(new GroupNode("Editable mesh"));
            _referenceMesh = sceneManager.RootNode.AddObject(new GroupNode("Reference meshs") { IsEditable = false});

            var cubeMesh = new CubeMesh(Scene.GraphicsDevice);
            _referenceMesh.AddObject(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(2, 0, 0), new Vector3(0.5f), "Item0", Scene.GraphicsDevice, false) );
            _editableMesh.AddObject(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(0, 0, 0), new Vector3(0.5f), "Item1", Scene.GraphicsDevice));
            _referenceMesh.AddObject(RenderItemHelper.CreateRenderItem(cubeMesh, new Vector3(-2, 0, 0), new Vector3(0.5f), "Item2", Scene.GraphicsDevice, false));

            if (MainFile != null)
            {
                var file = MainFile as PackFile;
                var m = new RmvRigidModel(file.DataSource.ReadData(), file.FullPath);
                var meshesLod0 = m.MeshList[0];
                foreach (var mesh in meshesLod0)
                {
                    var meshInstance = new Rmv2Geometry(mesh, Scene.GraphicsDevice);
                    var newItem = RenderItemHelper.CreateRenderItem(meshInstance, new Vector3(0, 0, 0), new Vector3(1.0f), mesh.Header.ModelName, Scene.GraphicsDevice);
                    _editableMesh.AddObject(newItem);
                }
            }

            // Wmd
            var refereneceMesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");

            SceneLoader loader = new SceneLoader(_packFileService, Scene.GraphicsDevice);
            var result = loader.Load(refereneceMesh as PackFile, null);
            _referenceMesh.AddObject(result);
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
