
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Scene;
using View3D.Utility;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class AnimationToolInput
    {
        public PackFile Mesh{ get; set; }
        public PackFile Animation { get; set; }
        public string FragmentName { get; set; }
        public AnimationSlotType AnimationSlot { get; set; }

        //AnimationSlotTypeHelper
        // Fragment
        // Slot
    }

    public abstract class BaseAnimationViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        bool _createDefaultAssets;
        protected PackFileService _pfs;
        protected SkeletonAnimationLookUpHelper _skeletonHelper;
        protected SchemaManager _schemaManager;
        public string DisplayName { get; set; } = "Creator";
        public PackFile MainFile { get; set; }

        protected IToolFactory _toolFactory;
        SceneContainer _scene;
        public SceneContainer Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public ReferenceModelSelectionViewModel MainModelView { get; set; }
        public ReferenceModelSelectionViewModel ReferenceModelView { get; set; }
        public AnimationPlayerViewModel Player { get; set; } = new AnimationPlayerViewModel();


        public AnimationToolInput MainInput { get; set; }

        public AnimationToolInput RefInput { get; set; }


        object _editor;
        public object Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        public BaseAnimationViewModel(IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, SchemaManager schemaManager, string headerAsset0, string headerAsset1, bool createDefaultAssets = true)
        {
            _toolFactory = toolFactory;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _schemaManager = schemaManager;
            _createDefaultAssets = createDefaultAssets;

            Scene = new SceneContainer();
            Scene.AddComponent(new FpsComponent(Scene));
            Scene.AddComponent(new KeyboardComponent(Scene));
            Scene.AddComponent(new MouseComponent(Scene));
            Scene.AddComponent(new ResourceLibary(Scene, pfs));
            Scene.AddComponent(skeletonHelper);
            Scene.AddComponent(schemaManager);
            Scene.AddComponent(new ArcBallCamera(Scene));
            Scene.AddComponent(new ClearScreenComponent(Scene));
            Scene.AddComponent(new RenderEngineComponent(Scene));
            Scene.AddComponent(new GridComponent(Scene));
            Scene.AddComponent(new SceneManager(Scene));
            Scene.AddComponent(new AnimationsContainerComponent(Scene));
            Scene.AddComponent(new SelectionManager(Scene));
            Scene.AddComponent(new SelectionComponent(Scene));
            Scene.AddComponent(new CommandExecutor(Scene));
            Scene.AddComponent(new LightControllerComponent(Scene));
            

            Scene.SceneInitialized += OnSceneInitialized;

            var mainAsset = Scene.AddComponent(new AssetViewModel(_pfs, headerAsset0, Color.Black, Scene));
            var refAsset = Scene.AddComponent(new AssetViewModel(_pfs, headerAsset1,  Color.Green, Scene));

            MainModelView = new ReferenceModelSelectionViewModel(_toolFactory, pfs, mainAsset, headerAsset0 + ":", Scene, skeletonHelper, schemaManager);
            ReferenceModelView = new ReferenceModelSelectionViewModel(_toolFactory, pfs, refAsset, headerAsset1 + ":", Scene, skeletonHelper, schemaManager);
        }

        private void OnSceneInitialized(WpfGame scene)
        {
            if (_createDefaultAssets)
            {
                Player.RegisterAsset(MainModelView.Data);
                Player.RegisterAsset(ReferenceModelView.Data);

                if (MainInput != null)
                {
                    MainModelView.Data.SetMesh(MainInput.Mesh);
                    if (MainInput.Animation != null)
                        MainModelView.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(MainInput.Animation, _pfs));
                }

                if (RefInput != null)
                {
                    ReferenceModelView.Data.SetMesh(RefInput.Mesh);
                    if (RefInput.Animation != null)
                        ReferenceModelView.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(RefInput.Animation, _pfs));
                }
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
