using System;
using System.Collections.Generic;
using System.Linq;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.ViewModels;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using View3D.Animation;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.SceneNodes;
using SkeletonBoneNode = AnimationEditor.Common.ReferenceModel.SkeletonBoneNode;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : NotifyPropertyChangedImpl, IHostedEditor<AnimationKeyframeEditorViewModel>
    {
        private SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private AnimationPlayerViewModel _animationPlayerViewModel;
        private SceneObjectBuilder _sceneObjectBuilder;
        private PackFileService _pfs;
        private ApplicationSettingsService _applicationSettings;
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private SelectionComponent _selectionComponent;
        private GizmoComponent _gizmoComponent;

        private CommandFactory _commandFactory;
        private SelectionManager _selectionManager;
        private TransformToolViewModel _transformToolViewModel;
        private CommandExecutor _commandExecutor;
        AnimationToolInput _inputRiderData;
        AnimationToolInput _inputMountData;
        private SceneObject _newAnimation;
        private SceneObject _mount;
        private SceneObject _rider;
        private ICommand _command;
        public NotifyAttr<bool> AllowToSelectAnimRoot { get; set; } = new NotifyAttr<bool>(false);

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set;  }

        public AnimationKeyframeEditorViewModel(PackFileService pfs,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionComponent selectionComponent,
            ApplicationSettingsService applicationSettings,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectBuilder sceneObjectBuilder,
            CommandFactory commandFactory,
            SelectionManager selectionManager,
            TransformToolViewModel transformToolViewModel,
            GizmoComponent gizmoComponent,
            CommandExecutor commandExecutor)
        {
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectBuilder = sceneObjectBuilder;
            _pfs = pfs;

            _applicationSettings = applicationSettings;

            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionComponent = selectionComponent;
            _commandFactory = commandFactory;
            _selectionManager = selectionManager;

            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
            _commandExecutor = commandExecutor;

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };
        }

        private void OnSelectionChanged(ISelectionState state, bool sendEvent)
        {
            if (state is BoneSelectionState boneSelectionState)
            {
                if (!AllowToSelectAnimRoot.Value)
                {
                    boneSelectionState.DeselectAnimRootNode();
                }
            }
        }

        private void UpdateCanSaveAndPreviewStates()
        {
        }

        private void OutputAnimationSetSelected(IAnimationBinGenericFormat newValue)
        {
        }

        public string EditorName => "Animations keyframe editor";

        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; private set; }
        public MountLinkViewModel MountLinkController { get; private set; }

        public void Initialize(EditorHost<AnimationKeyframeEditorViewModel> owner)
        {
            var riderItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Rider", Color.Black, _inputRiderData);
            var mountItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Mount", Color.Black, _inputMountData);
            mountItem.Data.IsSelectable = true;

            var propAsset = _sceneObjectBuilder.CreateAsset("New Anim", Color.Red);
            _animationPlayerViewModel.RegisterAsset(propAsset);

            Create(riderItem.Data, mountItem.Data, propAsset);
            owner.SceneObjects.Add(riderItem);
            owner.SceneObjects.Add(mountItem);
        }

        internal void Create(SceneObject rider, SceneObject mount, SceneObject newAnimation)
        {
            _newAnimation = newAnimation;
            _mount = mount;
            _rider = rider;

            _mount.SkeletonChanged += MountSkeletonChanged;
            _mount.AnimationChanged += TryReGenerateAnimation;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += TryReGenerateAnimation;

            MountLinkController = new MountLinkViewModel(_sceneObjectBuilder, _pfs, _skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            MountLinkController.ReloadFragments(false, true);
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            UpdateCanSaveAndPreviewStates();
            if (_newAnimation != null)
                _sceneObjectBuilder.SetAnimation(_newAnimation, null);
            
        }

        private void RiderSkeletonChanges(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                // ActiveOutputFragment.UpdatePossibleValues(null);
                SelectedRiderBone.UpdatePossibleValues(null);
            }
            else
            {
                //  ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadFragmentsForSkeleton(newValue.SkeletonName, true));
                SelectedRiderBone.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));
            }

            // Try setting using root bone
            SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            AnimationSettings.IsRootNodeAnimation = SelectedRiderBone.SelectedItem != null;

            // Try setting using hip bone
            if (AnimationSettings.IsRootNodeAnimation == false)
                SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("bn_hips", x.BoneName, StringComparison.OrdinalIgnoreCase));

            MountLinkController.ReloadFragments(true, false);
            UpdateCanSaveAndPreviewStates();
        }

        public void InsertNewFrame()
        {
            System.Windows.Forms.MessageBox.Show("insert new frame");
        }

        public void DuplicateFrame()
        {
            System.Windows.Forms.MessageBox.Show("DuplicateFrame");
        }

        private ISelectable FindSelectableObject(ISceneNode node)
        {
            if (node is ISelectable selectableNode) return selectableNode;
            foreach (var slot in node.Children)
            {
                return FindSelectableObject(slot);
            }
            return null;
        }

        public void EnterSelectMode()
        {
            if (_rider.MainNode.Children.Count == 0) return;

            var variantMeshRoot = _rider.MainNode.Children[1];
            if (variantMeshRoot.Children.Count == 0) return;
            var selectableNode = FindSelectableObject(variantMeshRoot);

            if (selectableNode != null)
            {
                _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(new List<ISelectable>() { selectableNode }, false, false)).BuildAndExecute();
                _selectionComponent.SetBoneSelectionMode();
                _rider.Player.Pause();
                _rider.Player.CurrentFrame++;
                if (_rider.Player.CurrentFrame + 1 == _rider.Player.FrameCount()) return;
                _rider.Player.CurrentFrame--;
            }

            _selectionManager.GetState().SelectionChanged += OnSelectionChanged;
        }

        public void UndoPose()
        {
            _commandExecutor.Undo();
        }
        public void EnterMoveMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;            
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Translate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);

        }

        public void EnterRotateMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Rotate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);

        }

        public void EnterScaleMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Scale);
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
        }

    }
}
