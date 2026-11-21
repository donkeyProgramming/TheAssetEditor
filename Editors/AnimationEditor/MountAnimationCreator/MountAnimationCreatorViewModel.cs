using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.MountAnimationCreator.ViewModels;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using Clipboard = System.Windows.Clipboard;

namespace AnimationEditor.MountAnimationCreator
{
    public static class MountToolIds
    {
        public static string Rider => "Rider";
        public static string Mount => "Mount";
        public static string Generated => "Generated";
    }

    /// <summary>
    /// The Rider Animation Editor, for mounted rider animation sets. Synchronized Mount Change tab: refits a synchronized
    /// rider set made for one mount onto another by gluing a rider bone to a vertex of the new mount. Attached Mount
    /// Change tab: moves an attached rider set onto another mount. Synchronized ↔ Attached tab: converts between the two.
    /// </summary>
    public class MountAnimationCreatorViewModel : EditorHostBase, IDisposable
    {
        public override Type EditorViewModelType => typeof(EditorView);
        private readonly IFileSaveService _fileSaveService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly IPackFileService _pfs;
        private readonly SelectionManager _selectionManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IGlobalEventHub _globalEventHub;
        private readonly IEditorManager _editorManager;
        private readonly LocalizationManager _loc;

        SceneObject _mount;
        SceneObject _rider;
        SceneObject _newAnimation;
        internal SceneObject Preview => _newAnimation;

        List<int> _mountVertexes = new();
        Rmv2MeshNode _mountVertexOwner;

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public AnimationOutputSettings Output { get; } = new();
        public RiderAndMountSets Sets { get; private set; }
        public AttachPointViewModel AttachPoint { get; private set; }
        public AttachedMountChangeViewModel AttachedMountChange { get; private set; }
        public AttachedRiderConversionViewModel AttachedRiders { get; set; }

        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanBatchProcess { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanSave { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> PreviewStatus { get; } = new("");
        public NotifyAttr<string> BatchStatus { get; } = new("");

        public NotifyAttr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAttr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAttr<string> SelectedVertexesText { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<int> SelectedModeTab { get; }
        public static int SynchronizedMountChangeTab => 0;
        public static int AttachedMountChangeTab => 1;
        public static int AttachConversionTab => 2;

        public MountAnimationCreatorViewModel(IEditorHostParameters editorHostParameters,
            IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionManager selectionManager,
            AnimationPlayerViewModel animationPlayerViewModel,
            IFileSaveService fileSaveService,
            IStandardDialogs standardDialogs,
            IGlobalEventHub globalEventHub,
            IEditorManager editorManager,
            LocalizationManager localizationManager) : base(editorHostParameters)
        {
            _standardDialogs = standardDialogs;
            DisplayName = "Rider Animation Editor";
            _animationPlayerViewModel = animationPlayerViewModel;
            _fileSaveService = fileSaveService;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = selectionManager;
            _globalEventHub = globalEventHub;
            _editorManager = editorManager;
            _loc = localizationManager;

            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.ShowSkeleton.Value = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.ShowMesh.Value = value; });
            SelectedModeTab = new NotifyAttr<int>(SynchronizedMountChangeTab, OnModeTabChanged);
            SelectedVertexesText.Value = _loc.Get("MountAnimationCreator.RiderAttachment.NoVertex");

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());

            AnimationSettings.SettingsChanged += () => TryReGenerateAnimation(null);

            Initialize();

            _globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => OnPackFilesChanged(x.AddedFiles));
            _globalEventHub.Register<PackFileContainerFilesUpdatedEvent>(this, x => OnPackFilesChanged(x.ChangedFiles));
            _globalEventHub.Register<PackFileContainerFilesRemovedEvent>(this, x => OnPackFilesChanged(x.RemovedFiles));
        }

        public void SetDebugInputParameters(AnimationToolInput rider, AnimationToolInput mount)
        {
            if (rider?.Mesh != null)
                _sceneObjectEditor.SetMesh(_rider, rider.Mesh);
            if (mount?.Mesh != null)
                _sceneObjectEditor.SetMesh(_mount, mount.Mesh);
        }

        void Initialize()
        {
            var riderItem = _sceneObjectViewModelBuilder.CreateAsset(MountToolIds.Rider, true, "Rider", Color.Black, null);
            var mountItem = _sceneObjectViewModelBuilder.CreateAsset(MountToolIds.Mount, true, "Mount", Color.Black, null);
            mountItem.Data.IsSelectable = true;

            var propAsset = _sceneObjectEditor.CreateAsset(MountToolIds.Generated, "Preview", Color.Red);
            _animationPlayerViewModel.RegisterAsset(propAsset);

            Sets = new RiderAndMountSets(riderItem.FragAndSlotSelection, mountItem.FragAndSlotSelection, UpdateCanSaveAndPreviewStates);

            Create(riderItem.Data, mountItem.Data, propAsset);
            SceneObjects.Add(riderItem);
            SceneObjects.Add(mountItem);
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

            AttachPoint = new AttachPointViewModel(_standardDialogs, _pfs, _loc);
            AttachedMountChange = new AttachedMountChangeViewModel(rider, mount, newAnimation, _sceneObjectEditor, _standardDialogs, _pfs, _fileSaveService, _skeletonAnimationLookUpHelper, Output, Sets, AttachPoint, _loc);
            AttachedRiders = new AttachedRiderConversionViewModel(rider, mount, newAnimation, _sceneObjectEditor, _standardDialogs, _pfs, _fileSaveService, _skeletonAnimationLookUpHelper, Output, Sets, AttachPoint, _loc);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
        }

        // The tabs share the preview rider, each filling it with its own result
        void OnModeTabChanged(int tab)
        {
            AttachedMountChange.IsActive = tab == AttachedMountChangeTab;
            AttachedRiders.IsActive = tab == AttachConversionTab;
            _newAnimation.Offset = Matrix.Identity;
            if (tab == AttachConversionTab)
                AttachedRiders.ClearPreview();
            else
                TryReGenerateAnimation(null);
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            UpdateCanSaveAndPreviewStates();
            if (SelectedModeTab.Value == AttachConversionTab)
            {
                AttachedRiders.OnSourceChanged();
                return;
            }
            if (SelectedModeTab.Value == AttachedMountChangeTab)
            {
                AttachedMountChange.OnSourceChanged();
                return;
            }

            if (CanPreview.Value)
                CreateMountAnimationAction();
            else if (_newAnimation != null)
                _sceneObjectEditor.SetAnimation(_newAnimation, null);
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
                SelectedRiderBone.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));

            // Try setting using root bone
            SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            AnimationSettings.IsRootNodeAnimation = SelectedRiderBone.SelectedItem != null;

            // Try setting using hip bone
            if (AnimationSettings.IsRootNodeAnimation == false)
                SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("bn_hips", x.BoneName, StringComparison.OrdinalIgnoreCase));

            UpdateCanSaveAndPreviewStates();
        }

        // New or changed animation sets show up in the set lists
        void OnPackFilesChanged(List<PackFile> files)
        {
            if (files.Any(x => x.Name.EndsWith(".animpack", StringComparison.OrdinalIgnoreCase)))
                Sets.Reload();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var missing = new List<string>();
            if (_rider?.AnimationClip == null || _rider.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.RiderAnimation"));
            if (_mount?.AnimationClip == null || _mount.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.MountAnimation"));
            if (SelectedRiderBone.SelectedItem == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.RiderBone"));
            if (_mountVertexes.Count == 0) missing.Add(_loc.Get("MountAnimationCreator.Needs.MountVertex"));
            CanPreview.Value = missing.Count == 0;
            CanSave.Value = missing.Count == 0;
            PreviewStatus.Value = Needs(missing);

            var batchMissing = new List<string>();
            if (Sets?.RiderSet == null) batchMissing.Add(_loc.Get("MountAnimationCreator.Needs.RiderSet"));
            if (Sets?.MountSet == null) batchMissing.Add(_loc.Get("MountAnimationCreator.Needs.MountSet"));
            if (SelectedRiderBone.SelectedItem == null) batchMissing.Add(_loc.Get("MountAnimationCreator.Needs.RiderBone"));
            if (_mountVertexes.Count == 0) batchMissing.Add(_loc.Get("MountAnimationCreator.Needs.MountVertex"));
            CanBatchProcess.Value = batchMissing.Count == 0;
            BatchStatus.Value = Needs(batchMissing);

            AttachedRiders?.UpdateStates();
            AttachedMountChange?.UpdateStates();
        }

        string Needs(List<string> missing) => missing.Count == 0 ? "" : _loc.Get("MountAnimationCreator.Needs") + " " + string.Join(", ", missing);

        public void SetMountVertex()
        {
            var state = _selectionManager.GetState<VertexSelectionState>();
            if (state == null || state.CurrentSelection().Count == 0)
            {
                SelectedVertexesText.Value = _loc.Get("MountAnimationCreator.RiderAttachment.NoVertex");
                _mountVertexes.Clear();
                _mountVertexOwner = null;
                _standardDialogs.ShowDialogBox(_loc.Get("MountAnimationCreator.RiderAttachment.NoVertexSelected"), _loc.Get("MountAnimationCreator.RiderAttachment.MountVertex"));
            }
            else
            {
                SelectedVertexesText.Value = string.Format(_loc.Get("MountAnimationCreator.RiderAttachment.VertexesSelected"), state.CurrentSelection().Count);
                _mountVertexOwner = state.RenderObject as Rmv2MeshNode;
                _mountVertexes = new List<int>(state.CurrentSelection());
            }

            UpdateCanSaveAndPreviewStates();
            TryReGenerateAnimation(null);
        }

        public void CreateMountAnimationAction()
        {
            var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);

            _sceneObjectEditor.CopyMeshFromOther(_newAnimation, _rider);
            _sceneObjectEditor.SetAnimationClip(_newAnimation, newRiderAnim, "Generated animation");
            _newAnimation.ShowSkeleton.Value = DisplayGeneratedSkeleton.Value;
            _newAnimation.ShowMesh.Value = DisplayGeneratedMesh.Value;
            UpdateCanSaveAndPreviewStates();
        }

        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.SelectedItem.BoneIndex, _rider, _mount);
        }

        /// <summary>Starts over with an empty tool, in the same tab position.</summary>
        public void ResetTool()
        {
            var answer = _standardDialogs.ShowYesNoBox(_loc.Get("MountAnimationCreator.Reset.Confirm"), _loc.Get("MountAnimationCreator.Buttons.Reset"));
            if (answer != ShowMessageBoxResult.OK)
                return;

            var fresh = _editorManager.Create(EditorEnums.MountTool_Editor);
            _editorManager.Drop(fresh, this, true);
            _editorManager.CloseTool(this);
            _editorManager.SetEditorAsCurrent(fresh);
        }

        public void SaveCurrentAnimationAction()
        {
            if (!CanSave.Value)
                return;
            var newAnimation = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);
            Output.Save(_pfs, _fileSaveService, _skeletonAnimationLookUpHelper, newAnimation, _rider.Skeleton, _rider.AnimationName.Value);
        }

        public void BatchProcessUsingFragmentsAction()
        {
            if (!CanBatchProcess.Value)
                return;

            var mountSet = Sets.MountSet;
            var riderSet = Sets.RiderSet;
            var name = _standardDialogs.ShowTextInputDialog(_loc.Get("MountAnimationCreator.Batch.NameTitle"), Output.SavePrefix.Value + riderSet.Name);
            if (name == null || !name.Result || string.IsNullOrWhiteSpace(name.Text))
                return;

            var generator = CreateAnimationGenerator();
            var service = new RiderSetBatchService(_pfs, _fileSaveService, _skeletonAnimationLookUpHelper, Output);
            var log = service.Process(riderSet, mountSet, name.Text.Trim(), slot =>
                MountAnimationGeneratorService.IsCopyOnlyAnimation(slot.RiderSlot)
                    ? RiderSlotResult.Keep("plays the same on any mount")
                    : new RiderSlotResult(generator.GenerateMountAnimation(slot.Mount, slot.Rider), ""));
            _standardDialogs.ShowErrorViewDialog(_loc.Get("MountAnimationCreator.Batch.ResultTitle"), log, false);
        }

        /// <summary>Copies every frame of the preview to the clipboard, in the Keyframe Editor's format.</summary>
        public void CopyAnimation()
        {
            if (_newAnimation.AnimationClip == null)
            {
                _standardDialogs.ShowDialogBox(_loc.Get("MountAnimationCreator.CopyForKeyframeEditor.NoPreview"), _loc.Get("MountAnimationCreator.Buttons.CopyRiderAnimationFrames"));
                return;
            }

            _newAnimation.Player.Pause();
            var endFrame = _newAnimation.AnimationClip.DynamicFrames.Count;
            var jsonText = JsonConvert.SerializeObject(AnimationCliboardCreator.CreateFrameClipboard(_newAnimation.Skeleton, _newAnimation.AnimationClip, 0, endFrame));
            Clipboard.SetText(jsonText);
        }

        public void Dispose()
        {
            _globalEventHub.UnRegister(this);

            if (_mount != null)
            {
                _mount.SkeletonChanged -= MountSkeletonChanged;
                _mount.AnimationChanged -= TryReGenerateAnimation;
            }

            if (_rider != null)
            {
                _rider.SkeletonChanged -= RiderSkeletonChanges;
                _rider.AnimationChanged -= TryReGenerateAnimation;
            }
        }
    }
}
