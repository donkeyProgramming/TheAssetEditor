using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFragment;
using CommonControls.Services;
using CommonControls.Table;
using FileTypes.AnimationPack;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace AnimationEditor.MountAnimationCreator
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();

        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanBatchProcess { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanAddToFragment { get; set; } = new NotifyAttr<bool>(false);
        

        public NotifyAttr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAttr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAttr<string> SelectedVertexesText { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SavePrefixText { get; set; } = new NotifyAttr<string>("new_");
        public NotifyAttr<bool> EnsureUniqeFileName { get; set; } = new NotifyAttr<bool>(true);
        
        public FilterCollection<AnimationFragment> ActiveOutputFragment { get; set; }
        public FilterCollection<FragmentStatusSlotItem> ActiveFragmentSlot { get; set; }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public MountLinkController MountLinkController { get; set; }

        AssetViewModel _mount;
        AssetViewModel _rider;
        AssetViewModel _newAnimation;
        List<int> _mountVertexes = new List<int>();
        Rmv2MeshNode _mountVertexOwner;
        PackFileService _pfs;
        SelectionManager _selectionManager;

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation, IComponentManager componentManager)
        {
            _pfs = pfs;
            _newAnimation = newAnimation;
            _mount = mount;
            _rider = rider;
            _selectionManager = componentManager.GetComponent<SelectionManager>();

            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.IsSkeletonVisible = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.MainNode.IsVisible = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());
            MountLinkController = new MountLinkController(pfs, skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            ActiveOutputFragment = new FilterCollection<AnimationFragment>(null, OutputFragmentSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; };

            ActiveFragmentSlot = new FilterCollection<FragmentStatusSlotItem>(null, (x)=> UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.Entry.Value.Slot.Value).Success; };

            _mount.SkeletonChanged += MountSkeletonChanged;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += RiderAnimationChanged;

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
        }

        private void RiderAnimationChanged(AnimationClip newValue)
        {
            UpdateCanSaveAndPreviewStates();
            if (CanPreview.Value)
                CreateMountAnimation();
            else
                _newAnimation?.SetAnimation(null);
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
                ActiveOutputFragment.UpdatePossibleValues(null);
                SelectedRiderBone.UpdatePossibleValues(null);
            }
            else
            {
                ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadFragmentsForSkeleton(newValue.SkeletonName, true));
                SelectedRiderBone.UpdatePossibleValues(SkeletonHelper.CreateFlatSkeletonList(newValue));
            }

            SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            MountLinkController.ReloadFragments(true, false);
            UpdateCanSaveAndPreviewStates();
        }

        void OutputFragmentSelected(AnimationFragment fragment)
        {
            if (fragment == null)
                ActiveFragmentSlot.UpdatePossibleValues(null);
            else
                ActiveFragmentSlot.UpdatePossibleValues(fragment.Fragments.Select(x=> new FragmentStatusSlotItem(x)));
            UpdateCanSaveAndPreviewStates();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var mountConnectionOk = SelectedRiderBone != null && _mountVertexes.Count != 0;
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK  && riderOK;
            CanBatchProcess.Value = MountLinkController?.SelectedMount?.SelectedItem != null && MountLinkController?.SelectedRider?.SelectedItem != null && mountConnectionOk;
            CanAddToFragment.Value = ActiveOutputFragment?.SelectedItem != null && ActiveFragmentSlot?.SelectedItem != null;// && CanPreview.Value;
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

        public void CreateMountAnimation()
        {
            var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);

            // Apply
            _newAnimation.CopyMeshFromOther(_rider, true);
            _newAnimation.SetAnimationClip(newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
            _newAnimation.IsSkeletonVisible = DisplayGeneratedSkeleton.Value;
            UpdateCanSaveAndPreviewStates();
        }

        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.SelectedItem.BoneIndex, _rider, _mount);
        }

        public void AddAnimationToFragment()
        {
            // Find stuff in active slot.
            var selectedAnimationSlot = MountLinkController.SelectedRiderTag.SelectedItem;

            AnimationClip newRiderClip = null;
            if (MountAnimationGeneratorService.IsCopyOnlyAnimation(selectedAnimationSlot.Entry.Value.Slot.Value))
                newRiderClip = _rider.AnimationClip;
            else
                newRiderClip = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);

            var fileResult = MountAnimationGeneratorService.SaveAnimation(_pfs, _rider.AnimationName.AnimationFile, SavePrefixText.Value, EnsureUniqeFileName.Value, newRiderClip, _newAnimation.Skeleton);
            if (fileResult == null)
                return;

            var newAnimSlot = selectedAnimationSlot.Entry.Value.Clone();
            newAnimSlot.AnimationFile = _pfs.GetFullPath(fileResult);
            newAnimSlot.Slot = ActiveFragmentSlot.SelectedItem.Entry.Value.Slot.Clone();

            var toRemove = ActiveOutputFragment.SelectedItem.Fragments.FirstOrDefault(x => x.Slot.Id == ActiveFragmentSlot.SelectedItem.Entry.Value.Slot.Id);
            ActiveOutputFragment.SelectedItem.Fragments.Remove(toRemove);

            ActiveOutputFragment.SelectedItem.Fragments.Add(newAnimSlot);

            var bytes = ActiveOutputFragment.SelectedItem.ParentAnimationPack.ToByteArray();
            SaveHelper.Save(_pfs, "animations\\animation_tables\\" + ActiveOutputFragment.SelectedItem.ParentAnimationPack.FileName, null, bytes, false);

            // Update status for the slot thing 
            var possibleValues = ActiveOutputFragment.SelectedItem.Fragments.Select(x => new FragmentStatusSlotItem(x));
            ActiveFragmentSlot.UpdatePossibleValues(possibleValues);
            MountLinkController.ReloadFragments(true, false);
        }

        public void ViewMountFragment()
        {
            ViewFragment(MountLinkController.SelectedMount.SelectedItem);
        }

        public void ViewRiderFragment()
        {
            ViewFragment(MountLinkController.SelectedRider.SelectedItem);
        }

        public void ViewOutputFragment()
        {
            ViewFragment(ActiveOutputFragment.SelectedItem, true);
        }

        void ViewFragment(AnimationFragment fragment, bool canEdit = false)
        {
            if (fragment != null)
            {
                var view = AnimationFragmentViewModel.CreateFromFragment(_pfs, fragment, canEdit);
                TableWindow.Show(view);
            }
        }

        public void RefreshView()
        {
            MountLinkController.ReloadFragments();
            ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadFragmentsForSkeleton(_rider.SkeletonName, true));
        }

        public void BatchProcess() 
        {
            var mountFrag = MountLinkController.SelectedMount.SelectedItem;
            var riderFrag = MountLinkController.SelectedRider.SelectedItem;

            var newFileName = "new_" + Path.GetFileNameWithoutExtension(riderFrag.FileName);
            var batchSettings = BatchProcessOptionsWindow.ShowDialog(newFileName, SavePrefixText.Value);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, CreateAnimationGenerator(), batchSettings);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments(true, false);

                ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadFragmentsForSkeleton(_rider.Skeleton.SkeletonName, true));
            }
        }
    }

    public class FragmentStatusSlotItem
    {
        public NotifyAttr<bool> IsValid { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<AnimationFragmentEntry> Entry { get; set; } = new NotifyAttr<AnimationFragmentEntry>(null);

        public FragmentStatusSlotItem(AnimationFragmentEntry entry)
        {
            Entry.Value = entry;
            IsValid.Value = !string.IsNullOrWhiteSpace(entry.AnimationFile);
        }
    }
}
