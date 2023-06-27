using AnimationEditor.AnimationTransferTool;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using Serilog;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using View3D.Animation;
using View3D.Components.Component.Selection;
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

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> DisplayGeneratedSkeleton { get;  set; } = new NotifyAttr<bool>();
        public NotifyAttr<bool> DisplayGeneratedMesh { get;  set; } = new NotifyAttr<bool>(); 
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get;  set; } 
        public MountLinkViewModel MountLinkController { get;  set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }
        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get;  set; }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();


        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation, IComponentManager componentManager, ApplicationSettingsService applicationSettings)
        {
            _pfs = pfs;
            _newAnimation = newAnimation;
            _applicationSettings = applicationSettings;
            _mount = mount;
            _rider = rider;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = componentManager.GetComponent<SelectionManager>();

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
    }
}
