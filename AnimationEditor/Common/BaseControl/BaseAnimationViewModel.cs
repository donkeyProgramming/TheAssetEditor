
using AnimationEditor.Common.AnimationPlayer;
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
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class AnimationToolInput
    {
        public PackFile Mesh{ get; set; }
        public PackFile Animation { get; set; }
    }

    public abstract class BaseAnimationViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        protected PackFileService _pfs;
        public string DisplayName { get; set; } = "Anim.Prop Creator";
        public IPackFile MainFile { get; set; }

        SceneContainer _scene;
        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public ReferenceModelSelectionViewModel MainModelView { get; set; }
        public ReferenceModelSelectionViewModel ReferenceModelView { get; set; }
        public AnimationPlayerViewModel Player { get; set; } = new AnimationPlayerViewModel();


        public AnimationToolInput MainInput { get; set; }

        public AnimationToolInput RefInput { get; set; }


        object _editor;
        public object Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        public BaseAnimationViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, string headerAsset0, string headerAsset1)
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
            Scene.AddCompnent(new SelectionManager(Scene));
            Scene.AddCompnent(new SelectionComponent(Scene));
            Scene.AddCompnent(new CommandExecutor(Scene));
            

            Scene.SceneInitialized += OnSceneInitialized;

            var mainAsset = Scene.AddCompnent(new AssetViewModel(_pfs, headerAsset0, Color.Black, Scene));
            var refAsset = Scene.AddCompnent(new AssetViewModel(_pfs, headerAsset1,  Color.Green, Scene));

            MainModelView = new ReferenceModelSelectionViewModel(pfs, mainAsset, headerAsset0 + ":", skeletonHelper);
            ReferenceModelView = new ReferenceModelSelectionViewModel(pfs, refAsset, headerAsset1 + ":", skeletonHelper);
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            Player.RegisterAsset(MainModelView.Data);
            Player.RegisterAsset(ReferenceModelView.Data);

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

            Initialize();
        }

        public virtual void Initialize()
        { 
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
