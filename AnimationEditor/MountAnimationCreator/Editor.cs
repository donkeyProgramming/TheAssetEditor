using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.MountAnimationCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Utility;

namespace AnimationEditor.MountAnimationCreator
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();

        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanBatchProcess { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanSave { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanAddToFragment { get; set; } = new NotifyAttr<bool>(false);
        

        public NotifyAttr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAttr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAttr<string> SelectedVertexesText { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SavePrefixText { get; set; } = new NotifyAttr<string>("new_");
        public ObservableCollection<uint> AnimationOutputFormats { get; set; } = new ObservableCollection<uint>() { 5,6,7};
        public NotifyAttr<uint> SelectedAnimationOutputFormat{ get; set; } = new NotifyAttr<uint>(7);
        public NotifyAttr<bool> EnsureUniqeFileName { get; set; } = new NotifyAttr<bool>(true);
        
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }
        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; set; }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public MountLinkViewModel MountLinkController { get; set; }

        AssetViewModel _mount;
        AssetViewModel _rider;
        AssetViewModel _newAnimation;
        private readonly ApplicationSettingsService _applicationSettings;
        List<int> _mountVertexes = new List<int>();
        Rmv2MeshNode _mountVertexOwner;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;
        PackFileService _pfs;
        SelectionManager _selectionManager;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public Editor(AssetViewModelBuilder assetViewModelBuilder, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SelectionManager selectionManager, ApplicationSettingsService applicationSettings)
        {
            _assetViewModelBuilder = assetViewModelBuilder;
            _pfs = pfs;
         
            _applicationSettings = applicationSettings;
     
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = selectionManager;

            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.ShowSkeleton.Value = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.ShowMesh.Value = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());
         

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };
            
            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x)=> UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };


           
            AnimationSettings.SettingsChanged += () => TryReGenerateAnimation(null);
        }

        internal Editor Create(AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation)
        {
            _newAnimation = newAnimation;
            _mount = mount;
            _rider = rider;

            _mount.SkeletonChanged += MountSkeletonChanged;
            _mount.AnimationChanged += TryReGenerateAnimation;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += TryReGenerateAnimation;

            MountLinkController = new MountLinkViewModel(_assetViewModelBuilder, _pfs, _skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);

            return this;
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            UpdateCanSaveAndPreviewStates();
            if (CanPreview.Value)
                CreateMountAnimationAction();
            else
            {
                if (_newAnimation != null)
                    _assetViewModelBuilder.SetAnimation(_newAnimation, null);
            }
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            UpdateCanSaveAndPreviewStates();
            MountLinkController.ReloadFragments(false, true);
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

        void OutputAnimationSetSelected(IAnimationBinGenericFormat animationSet)
        {
           if (animationSet == null)
               ActiveFragmentSlot.UpdatePossibleValues(null);
           else
               ActiveFragmentSlot.UpdatePossibleValues(animationSet.Entries);
            UpdateCanSaveAndPreviewStates();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var mountConnectionOk = SelectedRiderBone.SelectedItem != null && _mountVertexes.Count != 0;
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK  && riderOK;
            CanBatchProcess.Value = MountLinkController?.AnimationSetForMount?.SelectedItem != null && MountLinkController?.AnimationSetForRider?.SelectedItem != null && mountConnectionOk;
            CanAddToFragment.Value = ActiveOutputFragment?.SelectedItem != null && ActiveFragmentSlot?.SelectedItem != null;
            CanSave.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK && riderOK;
        }

        public void SetMountVertex()
        {
            var state = _selectionManager.GetState<VertexSelectionState>();
            if (state == null || state.CurrentSelection().Count == 0)
            {
                SelectedVertexesText.Value = "No vertex selected";
                _mountVertexes.Clear();
                _mountVertexOwner = null;
                MessageBox.Show(SelectedVertexesText.Value);
            }
            else
            {
                SelectedVertexesText.Value = $"{state.CurrentSelection().Count} vertexes selected";
                _mountVertexOwner = state.RenderObject as Rmv2MeshNode;
                _mountVertexes = new List<int>(state.CurrentSelection());
            }

            UpdateCanSaveAndPreviewStates();
        }

        public void CreateMountAnimationAction()
        {
            var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);

            // Apply
            _assetViewModelBuilder.CopyMeshFromOther(_newAnimation, _rider);
            _assetViewModelBuilder.SetAnimationClip(_newAnimation, newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
            _newAnimation.ShowSkeleton.Value = DisplayGeneratedSkeleton.Value;
            _newAnimation.ShowMesh.Value = DisplayGeneratedMesh.Value;
            UpdateCanSaveAndPreviewStates();
        }

        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.SelectedItem.BoneIndex, _rider, _mount);
        }

        public void AddAnimationToFragment()
        {
            // Find stuff in active slot.
            //var selectedAnimationSlot = MountLinkController.SelectedRiderTag.SelectedItem;
            //
            //AnimationClip newRiderClip = null;
            //if (MountAnimationGeneratorService.IsCopyOnlyAnimation(selectedAnimationSlot.SlotName))
            //    newRiderClip = _rider.AnimationClip;
            //else
            //    newRiderClip = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);
            //
            //var fileResult = MountAnimationGeneratorService.SaveAnimation(_pfs, _rider.AnimationName.Value.AnimationFile, SavePrefixText.Value, EnsureUniqeFileName.Value, newRiderClip, _newAnimation.Skeleton);
            //if (fileResult == null)
            //    return;
            //
            //var newAnimSlot = selectedAnimationSlot.Entry.Value.Clone();
            //newAnimSlot.AnimationFile = _pfs.GetFullPath(fileResult);
            //newAnimSlot.Slot = ActiveFragmentSlot.SelectedItem.Entry.Value.Slot.Clone();
            //
            //var toRemove = ActiveOutputFragment.SelectedItem.Fragments.FirstOrDefault(x => x.Slot.Id == ActiveFragmentSlot.SelectedItem.Entry.Value.Slot.Id);
            //ActiveOutputFragment.SelectedItem.Fragments.Remove(toRemove);
            //
            //ActiveOutputFragment.SelectedItem.Fragments.Add(newAnimSlot);
            //
            //var bytes = AnimationPackSerializer.ConvertToBytes(ActiveOutputFragment.SelectedItem.Parent);
            //SaveHelper.Save(_pfs, "animations\\animation_tables\\" + ActiveOutputFragment.SelectedItem.Parent.FileName, null, bytes, false);
            //
            //// Update status for the slot thing 
            //var possibleValues = ActiveOutputFragment.SelectedItem.Fragments.Select(x => new FragmentStatusSlotItem(x));
            //ActiveFragmentSlot.UpdatePossibleValues(possibleValues);
            //MountLinkController.ReloadFragments(true, false);
        }

       public void ViewMountFragmentAction() => ViewAnimationSet(MountLinkController.AnimationSetForMount.SelectedItem);
       public void ViewRiderFragmentAction() => ViewAnimationSet(MountLinkController.AnimationSetForRider.SelectedItem);
       public void ViewOutputFragmentAction() => ViewAnimationSet(ActiveOutputFragment.SelectedItem, true);

        void ViewAnimationSet(IAnimationBinGenericFormat animationSet, bool canEdit = false)
        {
            if (animationSet != null)
            {
                var animpackFileName = animationSet.PackFileReference.FileName;
                var packFile = _pfs.FindFile(animpackFileName);
                CommonControls.Editors.AnimationPack.AnimPackViewModel.ShowPreviewWinodow(packFile, _pfs, _skeletonAnimationLookUpHelper, animpackFileName, _applicationSettings);
            }
        }

        public void RefreshViewAction()
        {
           MountLinkController.ReloadFragments();
           ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.SkeletonName.Value, true));
        }

        public void SaveCurrentAnimationAction()
        {
            var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), new BatchProcessOptions { SavePrefix = SavePrefixText.Value}, SelectedAnimationOutputFormat.Value);
            service.SaveSingleAnim(_mount.AnimationClip, _rider.AnimationClip, _rider.AnimationName.Value.AnimationFile);
        }

        public void BatchProcessUsingFragmentsAction() 
        {
            var mountFrag = MountLinkController.AnimationSetForMount.SelectedItem;
            var riderFrag = MountLinkController.AnimationSetForRider.SelectedItem;
            
            var newFileName = SavePrefixText.Value + Path.GetFileNameWithoutExtension(riderFrag.FullPath);
            var batchSettings = BatchProcessOptionsWindow.ShowDialog(newFileName, SavePrefixText.Value);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), batchSettings, SelectedAnimationOutputFormat.Value);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments(true, false);
            
                ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.Skeleton.SkeletonName, true));
            }
        }


    }
}
