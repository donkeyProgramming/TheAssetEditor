using AnimationEditor.Common.AnimationSelector;
using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class MainPropCreatorViewModelInput
    {
        public PackFile Mesh{ get; set; }
        public PackFile Animation { get; set; }
    }

    public class MainPropCreatorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pfs;
        public string DisplayName { get; set; } = "Anim.Prop Creator";
        public IPackFile MainFile { get; set; }

        SceneContainer _scene;
        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public ReferenceModelSelectionViewModel MainModelView { get; set; }
        public ReferenceModelSelectionViewModel ReferenceModelView { get; set; }


        public MainPropCreatorViewModelInput MainInput { get; set; }

        public MainPropCreatorViewModelInput RefInput { get; set; }

        public PropCreatorEditorViewModel Editor { get; set; }

        public MainPropCreatorViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper)
        {
            _pfs = pfs;

            Scene = new SceneContainer();
            Scene.Components.Add(new FpsComponent(Scene));
            Scene.Components.Add(new KeyboardComponent(Scene));
            Scene.Components.Add(new MouseComponent(Scene));
            Scene.Components.Add(new ResourceLibary(Scene, pfs));
            Scene.Components.Add(new ArcBallCamera(Scene, new Vector3(0), 10));
            Scene.Components.Add(new ClearScreenComponent(Scene));
            Scene.Components.Add(new RenderEngineComponent(Scene));
            Scene.Components.Add(new GridComponent(Scene));
            Scene.Components.Add(new SceneManager(Scene));
            Scene.Components.Add(new AnimationsContainerComponent(Scene));

            Scene.SceneInitialized += OnSceneInitialized;

            MainModelView = new ReferenceModelSelectionViewModel(pfs, "Data: ", skeletonHelper);
            ReferenceModelView = new ReferenceModelSelectionViewModel(pfs, "Reference: ", skeletonHelper);
            Editor = new PropCreatorEditorViewModel(MainModelView.Data, ReferenceModelView.Data);
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            var sceneManager = scene.GetComponent<SceneManager>();
            var resourceLib = scene.GetComponent<ResourceLibary>();
            var animComp = scene.GetComponent<AnimationsContainerComponent>();

            MainModelView.Data.Initialize(resourceLib, animComp.RegisterAnimationPlayer(new AnimationPlayer(), "PlayerMain"), sceneManager.RootNode, Color.Black);
            ReferenceModelView.Data.Initialize(resourceLib, animComp.RegisterAnimationPlayer(new AnimationPlayer(), "PlayerRef"), sceneManager.RootNode, Color.Green);

            if (MainInput != null)
            {
                MainModelView.Data.SetMesh(MainInput.Mesh);
                MainModelView.Data.SetAnimation(MainInput.Animation);
            }

            if (RefInput != null)
            {
                ReferenceModelView.Data.SetMesh(RefInput.Mesh);
                ReferenceModelView.Data.SetAnimation(RefInput.Animation);
            }
        }

        public void Close()
        {
            Scene.Dispose();
            Scene.SceneInitialized -= OnSceneInitialized;
            Scene = null;
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }

        public bool Save()
        {
            return true;
        }
    }
}
