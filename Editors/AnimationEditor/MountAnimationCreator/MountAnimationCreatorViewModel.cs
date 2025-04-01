using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AnimationEditor.AnimationKeyframeEditor;
using System.Windows.Forms;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.MountAnimationCreator.ViewModels;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using MessageBox = System.Windows.Forms.MessageBox;
using Clipboard = System.Windows.Clipboard;
using Shared.Ui.Events.UiCommands;
using Shared.Core.Events;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using GameWorld.Core.Services;


namespace AnimationEditor.MountAnimationCreator
{
    public class MountAnimationCreatorViewModel : NotifyPropertyChangedImpl, IHostedEditor<MountAnimationCreatorViewModel>
    {
        public Type EditorViewModelType => typeof(EditorView);
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly IFileSaveService _fileSaveService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly IPackFileService _pfs;
        private readonly SelectionManager _selectionManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        SceneObject _mount;
        SceneObject _rider;
        SceneObject _newAnimation;
  
        List<int> _mountVertexes = new();
        Rmv2MeshNode _mountVertexOwner;

        AnimationToolInput _inputRiderData;
        AnimationToolInput _inputMountData;

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public MountLinkViewModel MountLinkController { get; set; }
        public string EditorName => "Mount Animation Creator";

        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanBatchProcess { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanSave { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanAddToFragment { get; set; } = new NotifyAttr<bool>(false);

        public NotifyAttr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAttr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAttr<string> SelectedVertexesText { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SavePrefixText { get; set; } = new NotifyAttr<string>("new_");
        public ObservableCollection<uint> AnimationOutputFormats { get; set; } = new ObservableCollection<uint>() { 5, 6, 7 };
        public NotifyAttr<uint> SelectedAnimationOutputFormat { get; set; } = new NotifyAttr<uint>(7);
        public NotifyAttr<bool> EnsureUniqeFileName { get; set; } = new NotifyAttr<bool>(true);

        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }
        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; set; }

        public MountAnimationCreatorViewModel(IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            SelectionManager selectionManager,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectEditor sceneObjectBuilder,
            IFileSaveService fileSaveService,
            IUiCommandFactory uiCommandFactory)
        {
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectBuilder = sceneObjectBuilder;
            _fileSaveService = fileSaveService;
            _uiCommandFactory = uiCommandFactory;
            _pfs = pfs;

            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = selectionManager;

            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.ShowSkeleton.Value = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.ShowMesh.Value = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());


            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };

            AnimationSettings.SettingsChanged += () => TryReGenerateAnimation(null);
        }

        public void SetDebugInputParameters(AnimationToolInput rider, AnimationToolInput mount)
        {
            _inputRiderData = rider;
            _inputMountData = mount;
        }

        public void Initialize(EditorHost<MountAnimationCreatorViewModel> owner)
        {
           // var riderItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Rider", Color.Black, _inputRiderData);
           // var mountItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Mount", Color.Black, _inputMountData);
           // mountItem.Data.IsSelectable = true;
           //
           // var propAsset = _sceneObjectBuilder.CreateAsset("New Anim", Color.Red);
           // _animationPlayerViewModel.RegisterAsset(propAsset);
           //
           // Create(riderItem.Data, mountItem.Data, propAsset);
           // owner.SceneObjects.Add(riderItem);
           // owner.SceneObjects.Add(mountItem);
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

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
           // UpdateCanSaveAndPreviewStates();
           // if (CanPreview.Value)
           //     CreateMountAnimationAction();
           // else
           // {
           //     if (_newAnimation != null)
           //         _sceneObjectBuilder.SetAnimation(_newAnimation, null);
           // }
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
            CanPreview.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK && riderOK;
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
           //var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);
           //
           //// Apply
           //_sceneObjectBuilder.CopyMeshFromOther(_newAnimation, _rider);
           //_sceneObjectBuilder.SetAnimationClip(_newAnimation, newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
           //_newAnimation.ShowSkeleton.Value = DisplayGeneratedSkeleton.Value;
           //_newAnimation.ShowMesh.Value = DisplayGeneratedMesh.Value;
           //UpdateCanSaveAndPreviewStates();
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
        public void ViewOutputFragmentAction() => ViewAnimationSet(ActiveOutputFragment.SelectedItem);

        void ViewAnimationSet(IAnimationBinGenericFormat animationSet)
        {
            if (animationSet != null)
            {
                var animpackFileName = animationSet.PackFileReference.FileName;
                _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(animpackFileName, 800, 900);
            }
        }

        public void RefreshViewAction()
        {
            MountLinkController.ReloadFragments();
            ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.SkeletonName.Value, true));
        }

        public void SaveCurrentAnimationAction()
        {
           // var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), new BatchProcessOptions { SavePrefix = SavePrefixText.Value }, _fileSaveService, SelectedAnimationOutputFormat.Value);
           // service.SaveSingleAnim(_mount.AnimationClip, _rider.AnimationClip, _rider.AnimationName.Value.AnimationFile);
        }

        public void BatchProcessUsingFragmentsAction()
        {
            var mountFrag = MountLinkController.AnimationSetForMount.SelectedItem;
            var riderFrag = MountLinkController.AnimationSetForRider.SelectedItem;

            var newFileName = SavePrefixText.Value + Path.GetFileNameWithoutExtension(riderFrag.FullPath);
            var batchSettings = BatchProcessOptionsWindow.ShowDialog(newFileName, SavePrefixText.Value);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), batchSettings, _fileSaveService, SelectedAnimationOutputFormat.Value);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments(true, false);

                ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.Skeleton.SkeletonName, true));
            }
        }

        public void CopyAnimation()
        {
            if (_newAnimation.AnimationClip == null)
            {
                MessageBox.Show("new animation not generated!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _newAnimation.Player.Pause();

            var currentFrame = 0;
            var endFrame = _newAnimation.AnimationClip.DynamicFrames.Count;
            var skeleton = _newAnimation.Skeleton;
            var frames = _newAnimation.AnimationClip;
            var jsonText = JsonConvert.SerializeObject(AnimationCliboardCreator.CreateFrameClipboard(skeleton, frames, currentFrame, endFrame));
            Clipboard.SetText(jsonText);
            MessageBox.Show($"copied frame {currentFrame} up to {endFrame - 1}", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}



