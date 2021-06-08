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
using System.Collections.ObjectModel;
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

        public NotifyAtr<bool> CanPreview { get; set; } = new NotifyAtr<bool>(false);
        public NotifyAtr<bool> CanSave { get; set; } = new NotifyAtr<bool>(false);
        public NotifyAtr<bool> CanPatchProcess { get; set; } = new NotifyAtr<bool>(false);

        public NotifyAtr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAtr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAtr<string> SelectedVertexesText { get; set; } = new NotifyAtr<string>("");
        public NotifyAtr<string> SavePrefixText { get; set; } = new NotifyAtr<string>("new_");


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

            DisplayGeneratedSkeleton = new NotifyAtr<bool>(true, (value) => _newAnimation.IsSkeletonVisible = value);
            DisplayGeneratedMesh = new NotifyAtr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.MainNode.IsVisible = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());
            MountLinkController = new MountLinkController(pfs, skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            _mount.SkeletonChanged += MountSkeletonChanged;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += RiderAnimationChanged;

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
        }

        private void RiderAnimationChanged(AnimationClip newValue)
        {
            UpdateCanSaveAndPreviewStates();
            if(CanSave.Value)
                CreateMountAnimation();
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            UpdateCanSaveAndPreviewStates();
        }

        private void RiderSkeletonChanges(GameSkeleton newValue)
        {
            if (newValue == null)
                SelectedRiderBone.UpdatePossibleValues(null);
            else
                SelectedRiderBone.UpdatePossibleValues(SkeletonHelper.CreateFlatSkeletonList(newValue));

            SelectedRiderBone.SelectedItem = SelectedRiderBone.Values.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            UpdateCanSaveAndPreviewStates();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview.Value = SelectedRiderBone != null && _mountVertexes.Count != 0 && mountOK  && riderOK;
            CanSave.Value = CanPreview.Value && _newAnimation.AnimationClip != null;
            CanPatchProcess.Value = false;
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
            _newAnimation.SetAnimationClip(newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("New mount animation", null));
            _newAnimation.IsSkeletonVisible = DisplayGeneratedSkeleton.Value;
            UpdateCanSaveAndPreviewStates();
        }

        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.SelectedItem.BoneIndex, _rider, _mount);
        }
        
        public void SaveAnimation()
        {
            MountAnimationGeneratorService.SaveAnimation(_pfs, _rider.AnimationName.AnimationFile, SavePrefixText.Value, _newAnimation.AnimationClip, _newAnimation.Skeleton);
        }

        public void AddAnimationToFragment()
        { }

        public void ViewMountFragment()
        {
            ViewFragment(MountLinkController.SelectedMount.SelectedItem?.Entry);
        }

        public void ViewRiderFragment()
        {
            ViewFragment(MountLinkController.SelectedRider.SelectedItem?.Entry);
        }

        void ViewFragment(AnimationFragment fragment)
        {
            if (fragment != null)
            {
                var view = AnimationFragmentViewModel.CreateFromFragment(_pfs, fragment, false);
                TableWindow.Show(view);
            }
        }

        public void BatchProcess()
        {
            var mountFrag = MountLinkController.SelectedMount.SelectedItem.Entry;
            var riderFrag = MountLinkController.SelectedRider.SelectedItem.Entry;
            
            var batchSettings = BatchProcessOptionsWindow.ShowDialog("new_" + Path.GetFileNameWithoutExtension(riderFrag.FileName), SavePrefixText.Value);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, CreateAnimationGenerator(), batchSettings);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments();
            }
        }
    }
}
