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


        CubeMesh _cubeMesh;
        public SceneViewModel()
        {
            DisplayName = "3d viewer";
            Scene = new SceneContainer();


            Scene.Camera = new ArcBallCamera(Scene, new Vector3(0), 10);

            Scene.Components.Add(new ResourceLibary(Scene));
            Scene.Components.Add(new KeyboardComponent(Scene));
            Scene.Components.Add(new MouseComponent(Scene));
            Scene.Components.Add(Scene.Camera);
            Scene.Components.Add(new PickingComponent(Scene));
            Scene.Components.Add(new SceneManager(Scene));
            Scene.Components.Add(new SelectionManager(Scene));
            Scene.Components.Add(new CommandManager(Scene));
            Scene.Components.Add(new GizmoComponent(Scene));
            Scene.SceneInitialized += OnSceneInitialized;
        }

        private void OnSceneInitialized(SceneContainer scene)
        {
            //var graphicsArgs = new GraphicsArgs(Scene.Camera, Scene.GraphicsDevice, new ResourceLibary(scene.Content));
            //var inputSystems = new InputSystems(scene.Mouse, scene.Keyboard);


            // _commandManager = new CommandManager(Scene.Keyboard);

            //_gizmo = new GizmoComponent(graphicsArgs, inputSystems, _selectionManager, _commandManager);
            //scene.Components.Add(_pickingController);
            //scene.Components.Add(_gizmo);

            var sceneManager = scene.GetComponent<SceneManager>();
            _cubeMesh = new CubeMesh(Scene.GraphicsDevice);
            sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(2,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item0" });
            sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(0,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item1" });
            sceneManager.RenderItems.Add(new RenderItem(_cubeMesh, new Vector3(-2,0,0), Quaternion.Identity, new Vector3(0.5f)) { Id = "Item2" });

            Scene.SceneManager = sceneManager;
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
