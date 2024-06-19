using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AnimationEditor.MountAnimationCreator.ViewModels;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using SkeletonBoneNode = Editors.Shared.Core.Common.ReferenceModel.SkeletonBoneNode;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : NotifyPropertyChangedImpl, IHostedEditor<AnimationKeyframeEditorViewModel>
    {
        public Type EditorViewModelType => typeof(EditorView);
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly SceneObjectBuilder _sceneObjectBuilder;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private GizmoToolbox _gizmoToolbox;
        private CopyPastePose _copyPastePose;
        private CopyPasteFromClipboardPose _copyPasteClipboardPose;
        private InterpolateBetweenPose _interpolateBetweenPose;

        public SelectionComponent SelectionComponent { get => _selectionComponent; private set { _selectionComponent = value; } }
        private SelectionComponent _selectionComponent;

        public GizmoComponent GizmoComponent { get => _gizmoComponent; private set { _gizmoComponent = value; } }
        private GizmoComponent _gizmoComponent;

        public CommandFactory CommandFactory { get => _commandFactory; private set { _commandFactory = value; } }
        private CommandFactory _commandFactory;

        public SelectionManager SelectionManager { get => _selectionManager; private set { _selectionManager = value; } }
        private SelectionManager _selectionManager;

        public CommandExecutor CommandExecutor { get => _commandExecutor; private set { _commandExecutor = value; } }
        private CommandExecutor _commandExecutor;

        private SceneObject _newAnimation;

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
                SetAndNotify(ref _copyMoreThanSingleFrame, value);
                EnableFrameNrStartTextboxOnPaste.Value = value && PasteUsingFormBelow;
            }
        }
        bool _copyMoreThanSingleFrame = false;

        public NotifyAttr<bool> DontWarnDifferentSkeletons { get; set; } = new(false);
        public NotifyAttr<bool> DontWarnIncomingFramesBigger { get; set;  } = new(false);
        public bool PasteUsingFormBelow 
        { 
            get => _pasteUsingFormBelow; 
            set 
            {
                SetAndNotify(ref _pasteUsingFormBelow, value);
                EnableFrameNrStartTextboxOnPaste.Value = !value && CopyMoreThanSingleFrame;
            } 
        }
        bool _pasteUsingFormBelow = false;

        public NotifyAttr<bool> PastePosition { get; set; } = new(true);
        public NotifyAttr<bool> PasteRotation { get; set; } = new(true);
        public NotifyAttr<bool> PasteScale { get; set; } = new(true);

        public NotifyAttr<bool> IsDirty { get; set; } = new(false);

        public NotifyAttr<bool> EnableFrameNrStartTextboxOnPaste { get; set; } = new(false);
        public NotifyAttr<bool> AutoSelectPreviousBonesOnFrameChange { get; set; } = new(false);

        public NotifyAttr<string> CurrentFrameNumber { get; set; } = new("");
        public NotifyAttr<string> TotalFrameNumber { get; set; } = new("");

        public string FrameNrLength { get => _frameNrLength; set => SetAndNotify(ref _frameNrLength, value); }
        private string _frameNrLength = "0";

        public string FramesDurationInSeconds
        {
            get => _txtEditDurationInSeconds;
            set
            {
                SetAndNotify(ref _txtEditDurationInSeconds, value);
            }
        }
        private string _txtEditDurationInSeconds = "";

        public NotifyAttr<string> SelectedFrameAInterpolation { get; set; } = new("Not set");
        public NotifyAttr<string> SelectedFrameBInterpolation { get; set; } = new("Not set");

        public bool PreviewInterpolation { 
            get => _previewInterpolation; 
            set 
            {
                SetAndNotify(ref _previewInterpolation, value);                
            }
        }
        private bool _previewInterpolation;

        public bool InterpolationOnlyOnSelectedBones
        {
            get => _interpolationOnlyOnSelectedBones;
            set
            {
                SetAndNotify(ref _interpolationOnlyOnSelectedBones, value);                
            }
        }
        private bool _interpolationOnlyOnSelectedBones;

        public float InterpolationValue
        {
            get => _interpolationValue;
            set
            {
                SetAndNotify(ref _interpolationValue, value);
                if(PreviewInterpolation) ApplyInterpolationOnCurrentFrame();
            }
        }
        private float _interpolationValue;


        public FilterCollection<SkeletonBoneNode> ModelBoneListForIKEndBone { get; set; } = new FilterCollection<SkeletonBoneNode>(null);
        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set;  }

        public AnimationKeyframeEditorViewModel(PackFileService pfs,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionComponent selectionComponent,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectBuilder sceneObjectBuilder,
            CommandFactory commandFactory,
            SelectionManager selectionManager,
            GizmoComponent gizmoComponent,
            CommandExecutor commandExecutor)
        {
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectBuilder = sceneObjectBuilder;
            _pfs = pfs;


            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionComponent = selectionComponent;
            _commandFactory = commandFactory;
            _selectionManager = selectionManager;

            _gizmoComponent = gizmoComponent;
            _commandExecutor = commandExecutor;

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };
            
        }

        private void OutputAnimationSetSelected(IAnimationBinGenericFormat newValue)
        {
            
        }

        public string EditorName => "Keyedframing Editor";

        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; private set; }
        public MountLinkViewModel MountLinkController { get; private set; }

        public void Initialize(EditorHost<AnimationKeyframeEditorViewModel> owner)
        {
            var riderItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Rider", Color.Black, null);
            var mountItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Mount", Color.Black, null);
            mountItem.Data.IsSelectable = true;

            var propAsset = _sceneObjectBuilder.CreateAsset("New Anim", Color.Red);
            _animationPlayerViewModel.RegisterAsset(propAsset);

            Create(riderItem.Data, mountItem.Data, propAsset);
            owner.SceneObjects.Add(riderItem);
            owner.SceneObjects.Add(mountItem);

            _gizmoToolbox = new(this);
            _copyPastePose = new(this);
            _copyPasteClipboardPose = new(this);
            _interpolateBetweenPose = new(this);
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
            _rider.Player.OnFrameChanged += RiderOnFrameChanged;            
        }

        private void UpdateCanSaveAndPreviewStates()
        {
            
        }

        private void RiderOnFrameChanged(int currentFrame)
        {            
            if(_rider.Player.AnimationClip != null)
            {
                CurrentFrameNumber.Value = _rider.Player.CurrentFrame.ToString();
                TotalFrameNumber.Value = _rider.Player.AnimationClip.DynamicFrames.Count.ToString();
            }
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            MountLinkController.ReloadFragments(false, true);
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            if (_newAnimation != null)
                _sceneObjectBuilder.SetAnimation(_newAnimation, null);

            if(newValue != null)
            {
                _originalClip = newValue.Clone();
                FramesDurationInSeconds = _originalClip.PlayTimeInSec.ToString();
                SetFrameLengthMax();
            }

            IsDirty.Value = false;
            _interpolateBetweenPose.Reset();
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
             _commandFactory.Create<DuplicateFrameBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, currentFrame)).BuildAndExecute();
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

            _commandFactory.Create<DeleteFrameBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, currentFrame)).BuildAndExecute();
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

        public void EnterScaleMode()
        {
            _gizmoToolbox.ScaleMode();
        }

        public void UndoPose()
        {
            Pause();
            _commandExecutor.Undo();
        }

        public void ResetPose()
        {
            var currentFrame = _rider.Player.CurrentFrame;
            if(_rider.AnimationClip == null || _originalClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var maximumFramesOriginal = _originalClip.DynamicFrames.Count;

            if(currentFrame > maximumFramesOriginal - 1)
            {
                MessageBox.Show("cannot reset the frame as this is a new frame!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            var command = _commandFactory.Create<ResetTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, _originalClip, currentFrame)).Build();
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
            if(CopyMoreThanSingleFrame)
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
            if(PasteUsingFormBelow)
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

        public void FirstFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            _rider.Player.CurrentFrame = 0;
            if (_mount.AnimationClip != null) _mount.Player.CurrentFrame = 0;
            if (AutoSelectPreviousBonesOnFrameChange.Value)
            {
                SelectPreviousBones();
            }
        }

        public void PrevFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            _rider.Player.CurrentFrame--;
            if (_mount.AnimationClip != null) _mount.Player.CurrentFrame--;
            if (AutoSelectPreviousBonesOnFrameChange.Value)
            {
                SelectPreviousBones();
            }
            EnableInterpolationSliderWithinRange();
        }
        public void NextFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Pause();
            _rider.Player.CurrentFrame++;
            if (_mount.AnimationClip != null) _mount.Player.CurrentFrame++;
            if (AutoSelectPreviousBonesOnFrameChange.Value)
            {
                SelectPreviousBones();
            }
            EnableInterpolationSliderWithinRange();
        }

        public void LastFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.CurrentFrame = _rider.AnimationClip.DynamicFrames.Count - 2;
            _rider.Player.CurrentFrame++;
            if (_mount.AnimationClip != null)
            {
                _mount.Player.CurrentFrame = _mount.AnimationClip.DynamicFrames.Count - 2;
                _mount.Player.CurrentFrame++;
            }
            Pause();
            if (AutoSelectPreviousBonesOnFrameChange.Value)
            {
                SelectPreviousBones();
            }
            ResetInterpolationSlider();
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

        public void ApplyInterpolationAcrossFrames()
        {
            MessageBox.Show("not available yet, in the meantime you can do it manually using the slider");
        }

        public void Save()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(!IsDirty.Value)
            {
                MessageBox.Show("there is nothing to save!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var animFile = _rider.AnimationClip.ConvertToFileFormat(_rider.Skeleton);
            var path = _rider.AnimationName.Value.AnimationFile;
            MessageBox.Show($"this will save with anim version {animFile.Header.Version}\n"+
                            $"on this path {path}\n", "warn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SaveHelper.Save(_pfs, path, null, AnimationFile.ConvertToBytes(animFile), true);
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
            SaveHelper.SaveAs(_pfs, bytes, ".anim");
            IsDirty.Value = false;
        }
    }
}
