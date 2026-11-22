using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.MountAnimationCreator.ViewModels;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using SkeletonBoneNode = Editors.Shared.Core.Common.ReferenceModel.SkeletonBoneNode;

namespace Editors.AnimationVisualEditors.AnimationKeyframeEditor
{
    public static class KeyframeEditorIds
    {
        public static string Rider => "Rider";
        public static string Mount => "Mount";
    }

    public class AnimationKeyframeEditorViewModel : EditorHostBase, IDisposable
    {
        public override Type EditorViewModelType => typeof(EditorView);
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly IPackFileService _pfs;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private GizmoToolbox _gizmoToolbox;
        private CopyPastePose _copyPastePose;
        private CopyPasteFromClipboardPose _copyPasteClipboardPose;
        private InterpolateBetweenPose _interpolateBetweenPose;

        public SelectionComponent SelectionComponent { get => _selectionComponent; private set { _selectionComponent = value; } }
        private SelectionComponent _selectionComponent;

        public GizmoComponent GizmoComponent { get => _gizmoComponent; private set { _gizmoComponent = value; } }
        private GizmoComponent _gizmoComponent;

        public IUiCommandFactory CommandFactory { get => _commandFactory; private set { _commandFactory = value; } }
        private IUiCommandFactory _commandFactory;

        public SelectionManager SelectionManager { get => _selectionManager; private set { _selectionManager = value; } }
        private SelectionManager _selectionManager;

        public CommandManager CommandManager { get => _commandExecutor; private set { _commandExecutor = value; } }
        private CommandManager _commandExecutor;
        private readonly IFileSaveService _packFileSaveService;
        private readonly IEventHub _eventHub;
        private readonly LocalizationManager _localizationManager;

        public SceneObject Mount { get => _mount; private set { _mount = value; } }
        private SceneObject _mount;

        public SceneObject Rider { get => _rider; private set { _rider = value; } }
        private SceneObject _rider;

        private AnimationClip _originalClip;

        public GameSkeleton Skeleton { get => _skeleton; private set { _skeleton = value; } }
        private GameSkeleton _skeleton;

        public NotifyAttr<bool> AllowToSelectAnimRoot { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> EnableInverseKinematics { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> IncrementFrameAfterCopyOperation { get; set; } = new NotifyAttr<bool>(false);

        public bool CopyMoreThanSingleFrame
        {
            get => _copyMoreThanSingleFrame;
            set
            {
                SetProperty(ref _copyMoreThanSingleFrame, value);
                EnableFrameNrStartTextboxOnPaste.Value = value && PasteUsingFormBelow;
            }
        }
        bool _copyMoreThanSingleFrame = false;

        public NotifyAttr<bool> DontWarnDifferentSkeletons { get; set; } = new(false);
        public NotifyAttr<bool> DontWarnIncomingFramesBigger { get; set; } = new(false);
        public bool PasteUsingFormBelow
        {
            get => _pasteUsingFormBelow;
            set
            {
                SetProperty(ref _pasteUsingFormBelow, value);
                EnableFrameNrStartTextboxOnPaste.Value = !value && CopyMoreThanSingleFrame;
            }
        }
        bool _pasteUsingFormBelow = false;

        public NotifyAttr<bool> PastePosition { get; set; } = new(true);
        public NotifyAttr<bool> PasteRotation { get; set; } = new(true);

        public NotifyAttr<bool> IsDirty { get; set; } = new(false);

        // The editing sections are greyed out until the model has an animation
        public NotifyAttr<bool> HasAnimation { get; set; } = new(false);

        public NotifyAttr<bool> EnableFrameNrStartTextboxOnPaste { get; set; } = new(false);
        public NotifyAttr<bool> AutoSelectPreviousBonesOnFrameChange { get; set; } = new(false);

        public NotifyAttr<string> CurrentFrameNumber { get; set; } = new("");
        public NotifyAttr<string> TotalFrameNumber { get; set; } = new("");
        public NotifyAttr<string> SelectedBonesText { get; set; } = new("");

        public string FrameNrLength { get => _frameNrLength; set => SetProperty(ref _frameNrLength, value); }
        private string _frameNrLength = "0";

        public string FramesDurationInSeconds
        {
            get => _txtEditDurationInSeconds;
            set
            {
                SetProperty(ref _txtEditDurationInSeconds, value);
            }
        }
        private string _txtEditDurationInSeconds = "";

        public NotifyAttr<string> SelectedFrameAInterpolation { get; set; } = new("-");
        public NotifyAttr<string> SelectedFrameBInterpolation { get; set; } = new("-");

        public bool PreviewInterpolation
        {
            get => _previewInterpolation;
            set
            {
                SetProperty(ref _previewInterpolation, value);
            }
        }
        private bool _previewInterpolation;

        public bool InterpolationOnlyOnSelectedBones
        {
            get => _interpolationOnlyOnSelectedBones;
            set
            {
                SetProperty(ref _interpolationOnlyOnSelectedBones, value);
            }
        }
        private bool _interpolationOnlyOnSelectedBones;

        public float InterpolationValue
        {
            get => _interpolationValue;
            set
            {
                SetProperty(ref _interpolationValue, value);
                if (PreviewInterpolation) ApplyInterpolationOnCurrentFrame();
            }
        }
        private float _interpolationValue;


        public FilterCollection<SkeletonBoneNode> ModelBoneListForIKEndBone { get; set; } = new FilterCollection<SkeletonBoneNode>(null);
        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }

        public AnimationKeyframeEditorViewModel(IEditorHostParameters editorHostParameters,
            IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionComponent selectionComponent,
            AnimationPlayerViewModel animationPlayerViewModel,
            IUiCommandFactory commandFactory,
            SelectionManager selectionManager,
            GizmoComponent gizmoComponent,
            CommandManager commandExecutor,
            IFileSaveService packFileSaveService,
            IEventHub eventHub,
            LocalizationManager localizationManager) : base(editorHostParameters)
        {
            DisplayName = "Keyframe Editor";
            _animationPlayerViewModel = animationPlayerViewModel;
            _pfs = pfs;


            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionComponent = selectionComponent;
            _commandFactory = commandFactory;
            _selectionManager = selectionManager;

            _gizmoComponent = gizmoComponent;
            _commandExecutor = commandExecutor;
            _packFileSaveService = packFileSaveService;
            _eventHub = eventHub;
            _localizationManager = localizationManager;

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };

            Initialize();
        }

        private void OutputAnimationSetSelected(IAnimationBinGenericFormat newValue)
        {

        }


        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; private set; }
        public MountLinkViewModel MountLinkController { get; private set; }

        // Off: one model, the one being edited. On: that model is a rider, and a mount stands next to it as a
        // reference. The mount is never edited, copied, pasted or saved
        public bool WithMount
        {
            get => _withMount;
            set
            {
                if (SetProperty(ref _withMount, value))
                {
                    OnPropertyChanged(nameof(SingleModel));
                    ApplyMountMode();
                }
            }
        }
        private bool _withMount;

        // For the other radio button
        public bool SingleModel
        {
            get => !_withMount;
            set
            {
                if (value)
                    WithMount = false;
            }
        }

        private SceneObjectViewModel _riderItem;
        private SceneObjectViewModel _mountItem;

        void Initialize()
        {
            // The mount is only registered with the animation controls while it's shown
            _riderItem = _sceneObjectViewModelBuilder.CreateAsset(KeyframeEditorIds.Rider, true, "Model", Color.Black, null);
            _mountItem = _sceneObjectViewModelBuilder.CreateAsset(KeyframeEditorIds.Mount, false, "Mount", Color.Black, null);
            _mountItem.Data.IsSelectable = true;
            _mountItem.IsVisible = false;

            Create(_riderItem.Data, _mountItem.Data);
            SceneObjects.Add(_riderItem);

            _gizmoToolbox = new(this);
            _eventHub.Register<SelectionChangedEvent>(this, OnSelectionStateChanged);
            UpdateSelectedBonesText();
            _copyPastePose = new(this);
            _copyPasteClipboardPose = new(this);
            _interpolateBetweenPose = new(this);

            // After the gizmo toolbox handler, which clears the bone selection on frame change
            _rider.Player.OnFrameChanged += RiderOnFrameStepped;
        }

        private void ApplyMountMode()
        {
            var name = _withMount ? "Rider" : "Model";
            _riderItem.HeaderName = name + ":";
            _rider.Description = name;
            Player.RefreshAsset(_rider);

            _mountItem.IsVisible = _withMount;
            if (_withMount)
            {
                // Registering restarts every animation from frame 0 when the controls are on; put a paused rider back
                var frame = _rider.Player.CurrentFrame;
                var wasPlaying = _rider.Player.IsPlaying;
                SceneObjects.Add(_mountItem);
                Player.RegisterAsset(_mount);
                if (!wasPlaying)
                {
                    Pause();
                    _rider.Player.CurrentFrame = frame;
                    if (_mount.AnimationClip != null)
                        _mount.Player.CurrentFrame = frame;
                }
            }
            else
            {
                _mount.Player.Pause();
                Player.UnregisterAsset(_mount);
                SceneObjects.Remove(_mountItem);
            }
        }

        internal void Create(SceneObject rider, SceneObject mount)
        {
            _mount = mount;
            _rider = rider;

            _mount.SkeletonChanged += MountSkeletonChanged;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += TryReGenerateAnimation;

            MountLinkController = new MountLinkViewModel(_sceneObjectEditor, _pfs, _skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
            _rider.Player.OnFrameChanged += RiderOnFrameChanged;
        }

        private void UpdateCanSaveAndPreviewStates()
        {

        }

        private void RiderOnFrameChanged(int currentFrame)
        {
            if (_rider.Player.AnimationClip != null)
            {
                CurrentFrameNumber.Value = _rider.Player.CurrentFrame.ToString();
                TotalFrameNumber.Value = _rider.Player.AnimationClip.DynamicFrames.Count.ToString();
            }
            else
            {
                CurrentFrameNumber.Value = "";
                TotalFrameNumber.Value = "";
            }
        }

        // A paused frame change: a step in the viewport controls, or a paste moving on a frame
        private void RiderOnFrameStepped(int currentFrame)
        {
            // The gizmo toolbox clears the selected bones on a frame change without an event
            UpdateSelectedBonesText();
            if (_rider.Player.IsPlaying || _rider.AnimationClip == null)
                return;

            if (AutoSelectPreviousBonesOnFrameChange.Value && GetSelectedBones()?.Count > 0)
                SelectPreviousBones();
            EnableInterpolationSliderWithinRange();
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            MountLinkController.ReloadFragments(false, true);
        }

        private void OnSelectionStateChanged(SelectionChangedEvent e)
        {
            _gizmoToolbox.HookState(e.NewState);
            UpdateSelectedBonesText();
        }

        // Bone selection keeps the animation and skeleton it started with; a new one makes it stale
        private void RefreshBoneSelection()
        {
            var state = _selectionManager.GetState<BoneSelectionState>();
            if (state == null)
                return;

            if (_rider.AnimationClip == null)
            {
                _selectionComponent.SetObjectSelectionMode();
                return;
            }

            state.CurrentAnimation = _rider.AnimationClip;
            state.Skeleton = _rider.Skeleton;
            state.CurrentFrame = _rider.Player.CurrentFrame;
            state.Clear();
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            RiderOnFrameChanged(_rider.Player.CurrentFrame);
            HasAnimation.Value = _rider.AnimationClip != null;
            RefreshBoneSelection();

            if (newValue != null)
            {
                _originalClip = newValue.Clone();
                FramesDurationInSeconds = _originalClip.PlayTimeInSec.ToString();
                SetFrameLengthMax();
            }

            IsDirty.Value = false;
            _interpolateBetweenPose?.Reset();
        }

        private void RiderSkeletonChanges(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ModelBoneListForIKEndBone.UpdatePossibleValues(null);
            }
            else
            {
                ModelBoneListForIKEndBone.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));
                ModelBoneListForIKEndBone.SelectedItem = ModelBoneListForIKEndBone.PossibleValues.FirstOrDefault(x => x.BoneName.ToLower() == "animroot");
            }

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
            _skeleton = newValue;
        }

        public List<int> GetSelectedBones() => _gizmoToolbox.PreviousSelectedBones;

        public List<int> GetModifiedBones() => _gizmoToolbox.ModifiedBones;

        public int GetModifiedFrameNr() => _gizmoToolbox.ModifiedFrameNr;

        public void DuplicateFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            _commandFactory.CreateWithBuilder<DuplicateFrameBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, currentFrame)).BuildAndExecute();
            _selectionComponent.SetBoneSelectionMode();
            _rider.Player.CurrentFrame++;
        }

        public void RemoveFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            var currentFrame = _rider.Player.CurrentFrame;

            var result = MessageBox.Show($"remove this frame at {currentFrame}? it is NOT recommended to remove a frame as this is a destructive operation", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            _commandFactory.CreateWithBuilder<DeleteFrameBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, currentFrame)).BuildAndExecute();
            _selectionComponent.SetBoneSelectionMode();
            _rider.Player.CurrentFrame++;
        }

        public void Pause()
        {
            _rider.Player.Pause();
            _mount.Player.Pause();
        }

        public void EnterSelectMode()
        {
            _gizmoToolbox.SelectMode();
        }

        public void SelectPreviousBones()
        {
            _gizmoToolbox.SelectPreviousBones();
        }

        public void EnterMoveMode()
        {
            _gizmoToolbox.MoveMode();
        }

        public void EnterRotateMode()
        {
            _gizmoToolbox.RotateMode();
        }

        public void UndoPose()
        {
            Pause();
            _commandExecutor.Undo();
        }

        public void ResetPose()
        {
            var currentFrame = _rider.Player.CurrentFrame;
            if (_rider.AnimationClip == null || _originalClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var maximumFramesOriginal = _originalClip.DynamicFrames.Count;

            if (currentFrame > maximumFramesOriginal - 1)
            {
                MessageBox.Show("cannot reset the frame as this is a new frame!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            var command = _commandFactory.CreateWithBuilder<ResetTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, _originalClip, currentFrame)).Build();
            _commandExecutor.ExecuteCommand(command);
        }

        public void CopyCurrentPose()
        {
            _copyPastePose.CopyCurrentPose();
        }

        public void PasteIntoCurrentFrame()
        {
            _copyPastePose.PasteIntoCurrentFrame();
        }

        public void PasteIntoSelectedCurrentNode()
        {
            _copyPastePose.PasteIntoSelectedCurrentNode();
        }

        public void PastePreviousEditedNodesIntoCurrentPose()
        {
            _copyPastePose.PastePreviousEditedNodesIntoCurrentPose();
        }
        private void CopyASingleFrameClipboard()
        {
            _copyPasteClipboardPose.CopySingle();
        }

        private void CopyMultipleFramesClipboard()
        {
            _copyPasteClipboardPose.CopyCurrentFrameUpToEndFrame();
        }

        public void CopyPoseInRangeToClipboard()
        {
            if (CopyMoreThanSingleFrame)
            {
                CopyMultipleFramesClipboard();
            }
            else
            {
                CopyASingleFrameClipboard();
            }

        }

        public void PasteASingleFrameClipboard()
        {
            _copyPasteClipboardPose.PasteSingle();
        }

        public void PasteMultipleFramesClipboardInRanges()
        {
            int.TryParse(FrameNrLength, out var pastedFramesLength);
            _copyPasteClipboardPose.PasteMultipleUpToRange(pastedFramesLength);
        }

        private void PasteMultipleFramesClipboardWhole()
        {
            _copyPasteClipboardPose.PasteAll();
        }

        public void PastePoseInRangeFromClipboard()
        {
            if (PasteUsingFormBelow)
            {
                PasteMultipleFramesClipboardInRanges();
            }
            else if (!PasteUsingFormBelow && CopyMoreThanSingleFrame)
            {
                PasteMultipleFramesClipboardWhole();
            }
            else
            {
                PasteASingleFrameClipboard();
            }

        }

        public void PasteIntoSelectedCurrentNodeFromClipboardWhole()
        {
            _copyPasteClipboardPose.PasteAllIntoSelectedNodes();
        }

        public void PasteIntoSelectedCurrentNodeFromClipboardInRange()
        {
            int.TryParse(FrameNrLength, out var pastedFramesLength);
            _copyPasteClipboardPose.PasteMultipleUpToRangeIntoSelectedBones(pastedFramesLength);
        }
        private void PasteASingleFrameIntoSelectedCurrentNodeFromClipboard()
        {
            _copyPasteClipboardPose.PasteSingleIntoSelectedBones();
        }

        public void PasteIntoInRangeSelectedCurrentNodeFromClipboard()
        {
            if (PasteUsingFormBelow)
            {
                PasteIntoSelectedCurrentNodeFromClipboardInRange();
            }
            else if (!PasteUsingFormBelow && CopyMoreThanSingleFrame)
            {
                PasteIntoSelectedCurrentNodeFromClipboardWhole();
            }
            else
            {
                PasteASingleFrameIntoSelectedCurrentNodeFromClipboard();
            }
        }

        public void ResetDuration()
        {
            FramesDurationInSeconds = _originalClip.PlayTimeInSec.ToString();
            _rider.AnimationClip.PlayTimeInSec = _originalClip.PlayTimeInSec;
        }


        public void ApplyDuration()
        {
            var validSeconds = float.TryParse(FramesDurationInSeconds, out var seconds);
            if (!validSeconds)
            {
                MessageBox.Show("please input proper decimal number in the textbox", "err", MessageBoxButtons.OK, MessageBoxIcon.Error);
                FramesDurationInSeconds = _rider.AnimationClip.PlayTimeInSec.ToString();
                return;
            }

            _rider.AnimationClip.PlayTimeInSec = seconds;
            IsDirty.Value = true;
        }

        public void NextFrame()
        {
            Pause();
            _rider.Player.CurrentFrame++;
            if (_mount.AnimationClip != null) _mount.Player.CurrentFrame++;
        }

        public void SetFrameLengthMax()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FrameNrLength = (_rider.AnimationClip.DynamicFrames.Count - 1).ToString();
        }

        public void SetFrameLengthFromClipboard()
        {
            FrameNrLength = _copyPasteClipboardPose.GetClipboardFramesLength().ToString();
        }

        public void SelectFrameAInterpolation()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var frameNr = _rider.Player.CurrentFrame;
            SelectedFrameAInterpolation.Value = frameNr.ToString();
            _interpolateBetweenPose.SelectFrameA(frameNr);
        }

        public void SelectFrameBInterpolation()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var frameNr = _rider.Player.CurrentFrame;
            SelectedFrameBInterpolation.Value = frameNr.ToString();
            _interpolateBetweenPose.SelectFrameB(frameNr);
        }

        private void EnableInterpolationSliderWithinRange()
        {
            if (!(_rider.Player.CurrentFrame > _interpolateBetweenPose.KeyFrameNrA && _rider.Player.CurrentFrame < _interpolateBetweenPose.KeyFrameNrB))
                ResetInterpolationSlider();
            else
                PreviewInterpolation = true;
        }

        public void ResetInterpolationTool()
        {
            ResetInterpolationSlider();
            InterpolationOnlyOnSelectedBones = false;
            _interpolateBetweenPose.Reset();
        }

        public void ResetInterpolationSlider()
        {
            PreviewInterpolation = false;
            InterpolationValue = 0;
        }

        public void ApplyInterpolationOnCurrentFrame()
        {
            _interpolateBetweenPose.ApplySingleFrame();
        }

        public void Save()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsDirty.Value)
            {
                MessageBox.Show("there is nothing to save!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var animFile = _rider.AnimationClip.ConvertToFileFormat(_rider.Skeleton);
            var path = _rider.AnimationName.Value;
            MessageBox.Show($"this will save with anim version {animFile.Header.Version}\n" +
                            $"on this path {path}\n", "warn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _packFileSaveService.Save(path, AnimationFile.ConvertToBytes(animFile), true);
            IsDirty.Value = false;
        }
        public void SaveAs()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var animFile = _rider.AnimationClip.ConvertToFileFormat(_rider.Skeleton);

            MessageBox.Show($"this will save with anim version {animFile.Header.Version}", "info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            _packFileSaveService.SaveAs(".anim", bytes);
            IsDirty.Value = false;
        }

        // Every bone selection change, undo included, comes through SelectionChangedEvent
        private void UpdateSelectedBonesText()
        {
            var state = _selectionManager.GetState<BoneSelectionState>();
            if (state == null)
            {
                SelectedBonesText.Value = _localizationManager.Get("AnimationKeyframeEditor.SelectedBones.Off");
                return;
            }

            var skeleton = state.Skeleton ?? _rider.Skeleton;
            // The gizmo toolbox drops animroot after this event when it isn't allowed
            var names = state.SelectedBones.Where(x => x != 0 || AllowToSelectAnimRoot.Value).Select(x => skeleton != null && x < skeleton.BoneCount ? skeleton.BoneNames[x] : x.ToString()).ToList();
            SelectedBonesText.Value = names.Count == 0
                ? _localizationManager.Get("AnimationKeyframeEditor.SelectedBones.None")
                : string.Join(", ", names);
        }

        public void Dispose()
        {
            _eventHub.UnRegister(this);

            if (_mount != null)
            {
                _mount.SkeletonChanged -= MountSkeletonChanged;
            }

            if (_rider != null)
            {
                _rider.SkeletonChanged -= RiderSkeletonChanges;
                _rider.AnimationChanged -= TryReGenerateAnimation;
                _rider.Player.OnFrameChanged -= RiderOnFrameChanged;
                _rider.Player.OnFrameChanged -= RiderOnFrameStepped;
            }
        }
    }
}
