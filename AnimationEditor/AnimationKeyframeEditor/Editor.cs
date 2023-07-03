using AnimationEditor.AnimationTransferTool;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using MonoGame.Framework.WpfInterop;
using Serilog;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using View3D.Animation;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.SceneNodes;
using SkeletonBoneNode = AnimationEditor.Common.ReferenceModel.SkeletonBoneNode;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();
        PackFileService _pfs;
        private AssetViewModel _newAnimation;
        private ApplicationSettingsService _applicationSettings;
        private AssetViewModel _mount;
        private AssetViewModel _rider;
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private SelectionManager _selectionManager;
        private SelectionComponent _selectionComponent;
        private TransformToolViewModel _transformToolVIewModel;
        private GizmoActions _gizmoActions;
        private CommandExecutor _commandManager;
        private SceneManager _sceneManager;

        private int _currentFrameNumber = 0;
        public int CurrentFrameNumber { get { return _currentFrameNumber; } set { SetAndNotifyWhenChanged(ref _currentFrameNumber, value); } }

        public NotifyAttr<bool> AllowToSelectAnimRoot { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> DisplayGeneratedSkeleton { get;  set; } = new NotifyAttr<bool>();
        public NotifyAttr<bool> DisplayGeneratedMesh { get;  set; } = new NotifyAttr<bool>(); 
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get;  set; } 
        public MountLinkViewModel MountLinkController { get;  set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }
        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get;  set; }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();


        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
                     AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation, 
                     IComponentManager componentManager, ApplicationSettingsService applicationSettings)
        {
            _pfs = pfs;
            _newAnimation = newAnimation;
            _applicationSettings = applicationSettings;
            _mount = mount;
            _rider = rider;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _commandManager = componentManager.GetComponent<CommandExecutor>();
            _selectionComponent = componentManager.GetComponent<SelectionComponent>();
            _sceneManager = componentManager.GetComponent<SceneManager>();
            
            _transformToolVIewModel = new TransformToolViewModel(componentManager);
            _gizmoActions = new GizmoActions(_transformToolVIewModel, componentManager);


            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.ShowSkeleton.Value = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.ShowMesh.Value = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());
            MountLinkController = new MountLinkViewModel(pfs, skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };

            _mount.SkeletonChanged += MountSkeletonChanged;
            _mount.AnimationChanged += TryReGenerateAnimation;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += TryReGenerateAnimation;

            AnimationSettings.SettingsChanged += () => TryReGenerateAnimation(null);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);

            _rider.Player.OnFrameChanged += OnFrameTick;
            _selectionManager.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            if(state is BoneSelectionState boneSelectionState)
            {
                if(!AllowToSelectAnimRoot.Value)
                {
                    boneSelectionState.DeselectAnimRootNode();
                }
            }
        }

        private void OnFrameTick(int currentFrame)
        {
            CurrentFrameNumber = currentFrame + 1;
        }

        

        private void OutputAnimationSetSelected(IAnimationBinGenericFormat animationSet)
        {
            if (animationSet == null)
                ActiveFragmentSlot.UpdatePossibleValues(null);
            else
                ActiveFragmentSlot.UpdatePossibleValues(animationSet.Entries);
            UpdateCanSaveAndPreviewStates();
        }

        private void UpdateCanSaveAndPreviewStates()
        {
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview.Value =  mountOK && riderOK;

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

        private void TryReGenerateAnimation(AnimationClip newValue)
        {
            UpdateCanSaveAndPreviewStates();
            if (CanPreview.Value)
                _newAnimation?.SetAnimation(null); //CreateMountAnimationAction();
            else
                _newAnimation?.SetAnimation(null);
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            UpdateCanSaveAndPreviewStates();
            MountLinkController.ReloadFragments(false, true);
        }

        public void InsertNewFrame()
        {
            MessageBox.Show("insert new frame");
        }

        public void DuplicateFrame()
        {
            MessageBox.Show("DuplicateFrame");
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
            if (_sceneManager.RootNode.Children.Count == 0) return;

            var root = _sceneManager.RootNode.Children[0];
            if (root.Children.Count < 2) return;
            var variantMeshRoot = root.Children[1];
            if (variantMeshRoot.Children.Count == 0) return;
            var selectableNode = FindSelectableObject(variantMeshRoot);

            if(selectableNode != null)
            {
                var selectCommand = new ObjectSelectionCommand(new List<ISelectable> { selectableNode }, false, false);
                _commandManager.ExecuteCommand(selectCommand);
                _selectionComponent.SetBoneSelectionMode();
                _rider.Player.Pause();
                _rider.Player.CurrentFrame++;
                if (_rider.Player.CurrentFrame + 1 == _rider.Player.FrameCount()) return;
                _rider.Player.CurrentFrame--;

            }
        }

        public void EnterMoveMode()
        {
            if(_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoActions.Move();
        }

        public void EnterRotateMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoActions.Rotate();
        }

        public void EnterScaleMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoActions.Scale();
        }

    }
}
