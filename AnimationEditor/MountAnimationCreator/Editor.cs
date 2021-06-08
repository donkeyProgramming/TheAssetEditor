using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimationFragment;
using CommonControls.ErrorListDialog;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CommonControls.Table;
using CsvHelper;
using Filetypes.RigidModel;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.ErrorListDialog.ErrorListViewModel;

namespace AnimationEditor.MountAnimationCreator
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();

        SkeletonBoneNode _selectedRiderBone;
        public SkeletonBoneNode SelectedRiderBone
        {
            get { return _selectedRiderBone; }
            set { SetAndNotify(ref _selectedRiderBone, value); UpdateCanSaveAndPreviewStates(); }
        }

        ObservableCollection<SkeletonBoneNode> _riderBones;
        public ObservableCollection<SkeletonBoneNode> RiderBones
        {
            get { return _riderBones; }
            set { SetAndNotify(ref _riderBones, value); }
        }

        SkeletonBoneNode _selectedMountBone;
        public SkeletonBoneNode SelectedMountBone
        {
            get { return _selectedMountBone; }
            set { SetAndNotify(ref _selectedMountBone, value); UpdateCanSaveAndPreviewStates(); }
        }

        ObservableCollection<SkeletonBoneNode> _mountBones;
        public ObservableCollection<SkeletonBoneNode> MountBones
        {
            get { return _mountBones; }
            set { SetAndNotify(ref _mountBones, value); }
        }

        bool _canPreview;
        public bool CanPreview
        {
            get { return _canPreview; }
            set { SetAndNotify(ref _canPreview, value); }
        }

        bool _canSave;
        public bool CanSave
        {
            get { return _canSave; }
            set { SetAndNotify(ref _canSave, value); }
        }

        bool _displayGeneratedRiderMesh = true;
        public bool DisplayGeneratedRiderMesh
        {
            get { return _displayGeneratedRiderMesh; }
            set { SetAndNotify(ref _displayGeneratedRiderMesh, value); UpdateRiderMeshVisability(value); }
        }

        bool _displayGeneratedRiderSkeleton = false;
        public bool DisplayGeneratedRiderSkeleton
        {
            get { return _displayGeneratedRiderSkeleton; }
            set { SetAndNotify(ref _displayGeneratedRiderSkeleton, value); UpdateRiderSkeletonVisability(value); }
        }

        string _selectedVertexesText;
        public string SelectedVertexesText
        {
            get { return _selectedVertexesText; }
            set { SetAndNotify(ref _selectedVertexesText, value); }
        }

        string _savePrefixText = "New_";
        public string SavePrefixText
        {
            get { return _savePrefixText; }
            set { SetAndNotify(ref _savePrefixText, value); }
        }  

        AssetViewModel _newAnimation;
        public AssetViewModel NewAnimation { get => _newAnimation; set => SetAndNotify(ref _newAnimation, value); }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public MountLinkController MountLinkController { get; set; }

        AssetViewModel _mount;
        AssetViewModel _rider;
        List<int> _mountVertexes = new List<int>();
        Rmv2MeshNode _mountVertexOwner;
        PackFileService _pfs;
        SelectionManager _selectionManager;

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation, IComponentManager componentManager)
        {
            _pfs = pfs;
            NewAnimation = newAnimation;
            _mount = mount;
            _rider = rider;

            _mount.SkeletonChanged += MountSkeletonChanged;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += RiderAnimationChanged;

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);

            _selectionManager = componentManager.GetComponent<SelectionManager>();

            MountLinkController = new MountLinkController(pfs, skeletonAnimationLookUpHelper,  rider, mount);
        }

        private void RiderAnimationChanged(AnimationClip newValue)
        {
            UpdateCanSaveAndPreviewStates();
            if(CanSave)
                CreateMountAnimation();
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
                MountBones = null;
            else
                MountBones = SkeletonHelper.CreateFlatSkeletonList(newValue);

            UpdateCanSaveAndPreviewStates();
        }

        private void RiderSkeletonChanges(GameSkeleton newValue)
        {
            if (newValue == null)
                RiderBones = null;
            else
                RiderBones = SkeletonHelper.CreateFlatSkeletonList(newValue);

            if(RiderBones != null)
                SelectedRiderBone = RiderBones.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            UpdateCanSaveAndPreviewStates();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview = SelectedRiderBone != null && _mountVertexes.Count != 0 && mountOK  && riderOK;
            CanSave = CanPreview && NewAnimation.AnimationClip != null;
        }

        public void SetMountVertex()
        {
            var state = _selectionManager.GetState<VertexSelectionState>();
            if (state == null || state.CurrentSelection().Count == 0)
            {
                SelectedVertexesText = "No vertex selected";
                _mountVertexes.Clear();
                _mountVertexOwner = null;
                MessageBox.Show(SelectedVertexesText);
            }
            else
            {
                SelectedVertexesText = $"{state.CurrentSelection().Count} vertexes selected";
                _mountVertexOwner = state.RenderObject as Rmv2MeshNode;
                _mountVertexes = new List<int>(state.CurrentSelection());
            }

            UpdateCanSaveAndPreviewStates();
        }

        public void CreateMountAnimation()
        {
            var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);

            // Apply
            NewAnimation.CopyMeshFromOther(_rider, true);
            NewAnimation.SetAnimationClip(newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("New mount animation", null));
            NewAnimation.IsSkeletonVisible = DisplayGeneratedRiderSkeleton;
            UpdateCanSaveAndPreviewStates();
        }


        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.BoneIndex, _rider, _mount);
        }

        private void UpdateRiderMeshVisability(bool value)
        {
            if (NewAnimation != null)
                NewAnimation.MainNode.IsVisible = value;
        }

        private void UpdateRiderSkeletonVisability(bool value)
        {
            if (NewAnimation != null)
                NewAnimation.IsSkeletonVisible = value;
        }
        
        public void SaveAnimation()
        {
            MountAnimationGeneratorService.SaveAnimation(_pfs, _rider.AnimationName.AnimationFile, SavePrefixText, NewAnimation.AnimationClip, NewAnimation.Skeleton);
        }

        public void AddAnimationToFragment()
        { }

        public void ViewMountFragment()
        {
            ViewFragment(MountLinkController.SelectedMount?.Entry);
        }

        public void ViewRiderFragment()
        {
            ViewFragment(MountLinkController.SeletedRider?.Entry);
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
            var mountFrag = MountLinkController.SelectedMount.Entry;
            var riderFrag = MountLinkController.SeletedRider.Entry;

            var batchSettings = BatchProcessOptionsWindow.ShowDialog("new_" + Path.GetFileNameWithoutExtension(riderFrag.FileName), SavePrefixText);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, CreateAnimationGenerator(), batchSettings);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments();
            }
        }
    }
}
