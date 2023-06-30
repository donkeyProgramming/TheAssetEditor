using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;
using View3D.Scene;
using View3D.Services;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class AnimationToolInput
    {
        public PackFile Mesh{ get; set; }
        public PackFile Animation { get; set; }
        public string FragmentName { get; set; }
        public AnimationSlotType AnimationSlot { get; set; }
    }

    public abstract class BaseAnimationViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public IServiceScope ServiceScope { get; set; }
        bool _createDefaultAssets;
        protected PackFileService _pfs;
        protected SkeletonAnimationLookUpHelper _skeletonHelper;
        protected ApplicationSettingsService _applicationSettingsService;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Creator");
        public PackFile MainFile { get; set; }

        protected IToolFactory _toolFactory;
        MainScene _scene;
        public MainScene Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public ReferenceModelSelectionViewModel MainModelView { get; set; }
        public ReferenceModelSelectionViewModel ReferenceModelView { get; set; }
        public AnimationPlayerViewModel Player { get; set; } = new AnimationPlayerViewModel();


        public AnimationToolInput MainInput { get; set; }

        public AnimationToolInput RefInput { get; set; }


        object _editor;
        public object Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        FocusSelectableObjectService _focusComponent;
        public ICommand ResetCameraCommand { get; set; }
        public ICommand FocusCamerasCommand { get; set; }

        string _headerAsset0; string _headerAsset1;

        public BaseAnimationViewModel(MainScene sceneContainer, IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
        {
            _toolFactory = toolFactory;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _applicationSettingsService = applicationSettingsService;

            Scene = sceneContainer;
            Scene.SceneInitialized += OnSceneInitialized;

            ResetCameraCommand = new RelayCommand(ResetCamera);
            FocusCamerasCommand = new RelayCommand(FocusCamera);
        }

        protected void Set(string headerAsset0, string headerAsset1, bool createDefaultAssets)
        {
            _headerAsset0 = headerAsset0;
            _headerAsset1 = headerAsset1;
            _createDefaultAssets = createDefaultAssets;
        }

        void ResetCamera() => _focusComponent.ResetCamera();
        void FocusCamera() => _focusComponent.FocusSelection();

        private void OnSceneInitialized(WpfGame scene)
        {
            var mainAsset = Scene.AddComponent(new AssetViewModel(_pfs, _headerAsset0, Color.Black, Scene, _applicationSettingsService));
            var refAsset = Scene.AddComponent(new AssetViewModel(_pfs, _headerAsset1, Color.Green, Scene, _applicationSettingsService));

            MainModelView = new ReferenceModelSelectionViewModel(_toolFactory, _pfs, mainAsset, _headerAsset0 + ":", Scene, _skeletonHelper, _applicationSettingsService);
            ReferenceModelView = new ReferenceModelSelectionViewModel(_toolFactory, _pfs, refAsset, _headerAsset1 + ":", Scene, _skeletonHelper, _applicationSettingsService);

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

        public bool HasUnsavedChanges { get; set; }

        public bool Save()
        {
            return true;
        }
    }
}
