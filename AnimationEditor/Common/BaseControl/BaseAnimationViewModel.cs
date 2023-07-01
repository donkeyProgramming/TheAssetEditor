using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;
using View3D.Animation.MetaData;
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

        private readonly MetaDataFactory _metaDataFactory;
        private readonly AssetViewModelEditor _assetViewModelBuilder;
        protected IToolFactory _toolFactory;
        MainScene _scene;
        public MainScene Scene { get => _scene; set => SetAndNotify(ref _scene, value); }

        public NotifyAttr<ReferenceModelSelectionViewModel> MainModelView { get; set; } = new NotifyAttr<ReferenceModelSelectionViewModel>();
        public NotifyAttr<ReferenceModelSelectionViewModel> ReferenceModelView { get; set; } = new NotifyAttr<ReferenceModelSelectionViewModel>();
        public AnimationPlayerViewModel Player { get; set; }


        public AnimationToolInput MainInput { get; set; }

        public AnimationToolInput RefInput { get; set; }


        object _editor;
        public object Editor { get => _editor; set => SetAndNotify(ref _editor, value); }

        FocusSelectableObjectService _focusComponent;
        public ICommand ResetCameraCommand { get; set; }
        public ICommand FocusCamerasCommand { get; set; }

        string _headerAsset0; string _headerAsset1;

        public BaseAnimationViewModel(AnimationPlayerViewModel animationPlayerViewModel, EventHub eventHub, MetaDataFactory metaDataFactory, AssetViewModelEditor assetViewModelBuilder, MainScene sceneContainer, IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService)
        {
            _toolFactory = toolFactory;
            _pfs = pfs;
            _skeletonHelper = skeletonHelper;
            _applicationSettingsService = applicationSettingsService;
            _metaDataFactory = metaDataFactory;
            _assetViewModelBuilder = assetViewModelBuilder;
            Scene = sceneContainer;
            Player = animationPlayerViewModel;

            ResetCameraCommand = new RelayCommand(ResetCamera);
            FocusCamerasCommand = new RelayCommand(FocusCamera);

            eventHub.Register<SceneInitializedEvent>(OnSceneInitialized);
        }

        protected void Set(string headerAsset0, string headerAsset1, bool createDefaultAssets)
        {
            _headerAsset0 = headerAsset0;
            _headerAsset1 = headerAsset1;
            _createDefaultAssets = createDefaultAssets;
        }

        void ResetCamera() => _focusComponent.ResetCamera();
        void FocusCamera() => _focusComponent.FocusSelection();

        private void OnSceneInitialized(SceneInitializedEvent scene)
        {
            MainModelView.Value = CreateAsset(_headerAsset0, Color.Black, MainInput);
            ReferenceModelView.Value = CreateAsset(_headerAsset1, Color.Black, RefInput);

            Initialize();
        }

        ReferenceModelSelectionViewModel CreateAsset(string header, Color skeletonColour, AnimationToolInput input)
        {
            var mainAsset = _assetViewModelBuilder.CreateAsset(header, skeletonColour);
            var returnObj = new ReferenceModelSelectionViewModel(_metaDataFactory, _toolFactory, _pfs, mainAsset, header + ":", Scene, _assetViewModelBuilder, _skeletonHelper, _applicationSettingsService);

            if (_createDefaultAssets)
            {
                Player.RegisterAsset(returnObj.Data);

                if (input != null)
                {
                    _assetViewModelBuilder.SetMesh(mainAsset, input.Mesh);
                    if (input.Animation != null)
                        _assetViewModelBuilder.SetAnimation(returnObj.Data, _skeletonHelper.FindAnimationRefFromPackFile(input.Animation, _pfs));
                }
            }
            return returnObj;
        }

        public void Close()
        {
            Scene = null;
        }

        public bool HasUnsavedChanges { get; set; }

        public bool Save() => true;

        public virtual void Initialize() { }
    }
}
