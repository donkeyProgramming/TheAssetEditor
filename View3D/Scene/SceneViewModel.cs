using Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Commands;
using View3D.Input;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene.Gizmo;

namespace View3D.Scene
{
    public class SceneViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public SceneContainer Scene { get; set; } 

        string _displayName;
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

        SceneManager _sceneManager;
        PickingComponent _pickingController;
        CommandManager _commandManager;
        SelectionManager _selectionManager;
        GizmoComponent _gizmo;


        CubeMesh _cubeMesh;
        public SceneViewModel()
        {
            DisplayName = "3d viewer";
            Scene = new SceneContainer();
            Scene.SceneInitialized += OnSceneInitialized;
        }

        private void OnSceneInitialized(SceneContainer scene)
        {
            var graphicsArgs = new GraphicsArgs(Scene.Camera, Scene.GraphicsDevice, new ResourceLibary(scene.Content));
            var inputSystems = new InputSystems(scene.Mouse, scene.Keyboard);

            _sceneManager = new SceneManager();         
            _selectionManager = new SelectionManager(_sceneManager);
            _commandManager = new CommandManager(Scene.Keyboard);
            _pickingController = new PickingComponent(graphicsArgs, inputSystems, _sceneManager, _selectionManager, _commandManager);
            _gizmo = new GizmoComponent(graphicsArgs, inputSystems, _selectionManager, _commandManager);

           
            scene.Components.Add(_pickingController);
            scene.Components.Add(_gizmo);

            _cubeMesh = new CubeMesh(Scene.GraphicsDevice);
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(2,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item0" });
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(0,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item1" });
            _sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(-2,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item2" });

            Scene.SceneManager = _sceneManager;
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
