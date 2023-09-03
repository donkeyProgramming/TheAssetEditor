using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator.ViewModels;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using View3D.Animation;
using View3D.Commands;
using View3D.Commands.Bone;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.SceneNodes;
using SkeletonBoneNode = AnimationEditor.Common.ReferenceModel.SkeletonBoneNode;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : NotifyPropertyChangedImpl, IHostedEditor<AnimationKeyframeEditorViewModel>
    {
        private SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private AnimationPlayerViewModel _animationPlayerViewModel;
        private SceneObjectBuilder _sceneObjectBuilder;
        private PackFileService _pfs;
        private ApplicationSettingsService _applicationSettings;
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private SelectionComponent _selectionComponent;
        private GizmoComponent _gizmoComponent;

        private CommandFactory _commandFactory;
        private SelectionManager _selectionManager;
        private TransformToolViewModel _transformToolViewModel;
        private CommandExecutor _commandExecutor;
        AnimationToolInput _inputRiderData;
        AnimationToolInput _inputMountData;
        private SceneObject _newAnimation;
        private SceneObject _mount;
        private SceneObject _rider;
        private AnimationClip _originalClip;
        private int _frameNrToCopy;


        private List<int> _previousSelectedBones;
        private List<int> _modifiedBones = new();
        private int _modifiedFrameNr = 0;

        public NotifyAttr<bool> AllowToSelectAnimRoot { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> EnableInverseKinematics { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> IncrementFrameAfterCopyOperation { get; set; } = new NotifyAttr<bool>(false);

        public NotifyAttr<bool> CopyMoreThanSingleFrame { get; set; } = new(false);
        public NotifyAttr<bool> DontWarnDifferentSkeletons { get; set; } = new(false);
        public NotifyAttr<bool> DontWarnIncomingFramesBigger { get; set;  } = new(false);
        public NotifyAttr<bool> PasteUsingFormBelow { get; set; } = new(false);
        public NotifyAttr<bool> PastePosition { get; set; } = new(true);
        public NotifyAttr<bool> PasteRotation { get; set; } = new(true);
        public NotifyAttr<bool> PasteScale { get; set; } = new(true);


        public string FrameNrStart { get =>_txtFrameNrStart; set => SetAndNotify(ref _txtFrameNrStart, value); }
        private string _txtFrameNrStart = "0";

        public string FrameNrEnd { get => _txtFrameNrEnd; set => SetAndNotify(ref _txtFrameNrEnd, value); }
        private string _txtFrameNrEnd = "";

        public string FramesDurationInSeconds
        {
            get { return _txtEditDurationInSeconds; }
            set
            {
                SetAndNotify(ref _txtEditDurationInSeconds, value);
            }
        }
        private string _txtEditDurationInSeconds = "";



        public FilterCollection<SkeletonBoneNode> ModelBoneListForIKEndBone { get; set; } = new FilterCollection<SkeletonBoneNode>(null);


        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }
        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set;  }

        public AnimationKeyframeEditorViewModel(PackFileService pfs,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionComponent selectionComponent,
            ApplicationSettingsService applicationSettings,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectBuilder sceneObjectBuilder,
            CommandFactory commandFactory,
            SelectionManager selectionManager,
            TransformToolViewModel transformToolViewModel,
            GizmoComponent gizmoComponent,
            CommandExecutor commandExecutor)
        {
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectBuilder = sceneObjectBuilder;
            _pfs = pfs;

            _applicationSettings = applicationSettings;

            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionComponent = selectionComponent;
            _commandFactory = commandFactory;
            _selectionManager = selectionManager;

            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
            _commandExecutor = commandExecutor;

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());

            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<AnimationBinEntryGenericFormat>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };

        }
        private void OnSelectionChanged(ISelectionState state, bool sendEvent)
        {
            if (state is BoneSelectionState boneSelectionState)
            {
                if(_previousSelectedBones == null || boneSelectionState.SelectedBones.Count > 0)
                {
                    _previousSelectedBones = new List<int>(boneSelectionState.SelectedBones);
                }

                if (!AllowToSelectAnimRoot.Value)
                {
                    boneSelectionState.DeselectAnimRootNode();
                }

                boneSelectionState.EnableInverseKinematics = EnableInverseKinematics.Value;
                boneSelectionState.InverseKinematicsEndBoneIndex = ModelBoneListForIKEndBone.SelectedItem.BoneIndex;

                if(boneSelectionState.EnableInverseKinematics)
                {
                    if(boneSelectionState.SelectedBones.Count > 1)
                    {
                        MessageBox.Show("when in IK mode is enabled, pick only only 1 bone. deselected the rest of the bones.", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        var firstSelection = boneSelectionState.SelectedBones[0];
                        boneSelectionState.SelectedBones.Clear();
                        boneSelectionState.SelectedBones.Add(firstSelection);
                        return;
                    }

                    if(boneSelectionState.SelectedBones.Count == 1 && boneSelectionState.InverseKinematicsEndBoneIndex == boneSelectionState.SelectedBones[0])
                    {
                        MessageBox.Show("head bone chain == tail bone chain. why even enable IK mode?", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        boneSelectionState.SelectedBones.Clear();
                    }
                }

                if(boneSelectionState.SelectedBones.Count == 0)
                {
                    _gizmoComponent.Disable();
                }
            }
        }

        private void OnModifiedBonesEvent(BoneSelectionState state)
        {
            if(_modifiedFrameNr == state.CurrentFrame)
            {
                _modifiedBones = _modifiedBones.Union(state.ModifiedBones).ToList();
            }
            else
            {
                _modifiedBones = state.ModifiedBones;
            }

            _modifiedFrameNr = state.CurrentFrame;
        }

        private void UpdateCanSaveAndPreviewStates()
        {
        }

        private void OutputAnimationSetSelected(IAnimationBinGenericFormat newValue)
        {
        }

        public string EditorName => "Keyedframing Editor";

        public FilterCollection<AnimationBinEntryGenericFormat> ActiveFragmentSlot { get; private set; }
        public MountLinkViewModel MountLinkController { get; private set; }

        public void Initialize(EditorHost<AnimationKeyframeEditorViewModel> owner)
        {
            var riderItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Rider", Color.Black, _inputRiderData);
            var mountItem = _sceneObjectViewModelBuilder.CreateAsset(true, "Mount", Color.Black, _inputMountData);
            mountItem.Data.IsSelectable = true;

            var propAsset = _sceneObjectBuilder.CreateAsset("New Anim", Color.Red);
            _animationPlayerViewModel.RegisterAsset(propAsset);

            Create(riderItem.Data, mountItem.Data, propAsset);
            owner.SceneObjects.Add(riderItem);
            owner.SceneObjects.Add(mountItem);
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

        private void RiderOnFrameChanged(int currentFrame)
        {

        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            MountLinkController.ReloadFragments(false, true);
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
            UpdateCanSaveAndPreviewStates();
            if (_newAnimation != null)
                _sceneObjectBuilder.SetAnimation(_newAnimation, null);

            if(newValue != null)
            {
                _originalClip = newValue.Clone();
                FramesDurationInSeconds = _originalClip.PlayTimeInSec.ToString();
                SetStartFrameZero();
                SetEndFrameMax();
            }


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
            UpdateCanSaveAndPreviewStates();
            
        }

        public void InsertNewFrame()
        {
            System.Windows.Forms.MessageBox.Show("insert new frame not implemented yet");
        }

        public void DuplicateFrame()
        {
            System.Windows.Forms.MessageBox.Show("DuplicateFrame not implemented yet");
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

        private void EnsureTheObjectsAreNotSelectable(ISceneNode node)
        {
            foreach (var slot in node.Children)
            {
                slot.IsEditable = false;
                EnsureTheObjectsAreNotSelectable(slot);
            }
        }

        public void EnterSelectMode()
        {
            if (_rider.MainNode.Children.Count <= 1) return;

            var variantMeshRoot = _rider.MainNode.Children[1];
            if (variantMeshRoot.Children.Count == 0) return;
            var selectableNode = FindSelectableObject(variantMeshRoot);
            EnsureTheObjectsAreNotSelectable(selectableNode);

            if (selectableNode != null)
            {
                _commandFactory.Create<ObjectSelectionCommand>().Configure(x => x.Configure(new List<ISelectable>() { selectableNode }, false, false)).BuildAndExecute();
                _selectionComponent.SetBoneSelectionMode();
                _rider.Player.Pause();
                _rider.Player.CurrentFrame++;
                if (_rider.Player.CurrentFrame + 1 == _rider.Player.FrameCount()) return;
                _rider.Player.CurrentFrame--;
            }

            _selectionManager.GetState().SelectionChanged += OnSelectionChanged;

            if(_selectionManager.GetState() is BoneSelectionState state)
            {
                state.BoneModifiedEvent += OnModifiedBonesEvent;
            }
        }

        public void SelectPreviousBones()
        {
            if (_rider.AnimationClip == null || _originalClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if(_previousSelectedBones == null)
            {
                MessageBox.Show("select a bone first!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EnterSelectMode();
            _commandFactory.Create<BoneSelectionCommand>().Configure(x => x.Configure(_previousSelectedBones, true, false)).BuildAndExecute();
        }

        public void UndoPose()
        {
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

            _rider.Player.Pause();
            var command = _commandFactory.Create<ResetTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip, _originalClip, currentFrame)).Build();
            command.ResetCurrentFrame();
            _commandExecutor.ExecuteCommand(command);
        }

        public void EnterMoveMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;            
            _gizmoComponent.ResetScale();
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);

        }

        public void EnterRotateMode()
        {
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoComponent.ResetScale();
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);

        }

        public void EnterScaleMode()
        {
            if(EnableInverseKinematics.Value)
            {
                MessageBox.Show("cannot use scale mode when IK is enabled!", "error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectionManager.GetState().Mode != GeometrySelectionMode.Bone) return;
            _gizmoComponent.ResetScale();
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
        }

        public void CopyCurrentPose()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            _frameNrToCopy = _rider.Player.CurrentFrame;
            if(IncrementFrameAfterCopyOperation.Value)
            {
                _rider.Player.CurrentFrame++;
            }
        }

        public void PasteIntoCurrentFrame()
        {
            var currentFrame = _rider.Player.CurrentFrame;
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var command = _commandFactory.Create<PasteTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip.DynamicFrames[_frameNrToCopy], 
                _rider.AnimationClip, currentFrame, currentFrame, null, 
                PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
            command.PasteWholeFrame();
            _commandExecutor.ExecuteCommand(command);
        }


        public void PasteIntoSelectedCurrentNode()
        {
            var currentFrame = _rider.Player.CurrentFrame;
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(_previousSelectedBones == null || _previousSelectedBones.Count() == 0)
            {
                MessageBox.Show("no bones were selected", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            _rider.Player.Pause();
            var command = _commandFactory.Create<PasteTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip.DynamicFrames[_frameNrToCopy],
                _rider.AnimationClip, currentFrame, currentFrame, _previousSelectedBones,
                PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
            command.PasteIntoSelectedBones();
            _commandExecutor.ExecuteCommand(command);
        }

        public void PastePreviousEditedNodesIntoCurrentPose()
        {
            var currentFrame = _rider.Player.CurrentFrame;
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_modifiedBones == null || _modifiedBones.Count() == 0)
            {
                MessageBox.Show("no bones were modified", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var command = _commandFactory.Create<PasteTransformBoneCommand>().Configure(x => x.Configure(_rider.AnimationClip.DynamicFrames[_modifiedFrameNr],
                _rider.AnimationClip, currentFrame, currentFrame, _modifiedBones,
                PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
            command.PasteIntoSelectedBones();
            _commandExecutor.ExecuteCommand(command);

            if (IncrementFrameAfterCopyOperation.Value)
            {
                _rider.Player.CurrentFrame++;
            }
        }

        public PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData CreateFrameClipboard(GameSkeleton skeleton, AnimationClip currentFrames, int startFrame, int endFrame)
        {
            var output = new PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData();

            output.SkeletonName = skeleton.SkeletonName;

            for (int frameNr = startFrame; frameNr < endFrame; frameNr++)
            {

                var frames = new PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData.Frame();
                for (int boneId = 0; boneId < currentFrames.DynamicFrames[frameNr].Position.Count; boneId++)
                {
                    var transform = currentFrames.DynamicFrames[frameNr];
                    var boneName = skeleton.GetBoneNameByIndex(boneId);

                    frames.BoneIdToPosition.Add  (boneName, transform.Position[boneId]);
                    frames.BoneIdToQuaternion.Add(boneName, transform.Rotation[boneId]);
                    frames.BoneIdToScale.Add     (boneName, transform.Scale[boneId]);
                }                
                output.Frames.Add(frameNr, frames);
            }

            return output;
        }

        private void CopyASingleFrameClipboard()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frames = _rider.AnimationClip;
            var jsonText = JsonConvert.SerializeObject(CreateFrameClipboard(skeleton, frames, currentFrame, currentFrame));
            Clipboard.SetText(jsonText);
        }

        private void CopyMultipleFramesClipboard()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int.TryParse(FrameNrStart, out var frameNrStart);
            int.TryParse(FrameNrEnd, out var frameNrEnd);

            if(frameNrStart > frameNrEnd)
            {
                MessageBox.Show("invalid input frameNrStart > frameNrEnd", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(frameNrStart < 0)
            {
                MessageBox.Show("frame nr start is invalid (negatif number)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var totalFrameNr = _rider.AnimationClip.DynamicFrames.Count;
            if (frameNrEnd > totalFrameNr)
            {
                MessageBox.Show("frame nr end is invalid (bigger than total frame number)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frames = _rider.AnimationClip;
            var jsonText = JsonConvert.SerializeObject(CreateFrameClipboard(skeleton, frames, frameNrStart, frameNrEnd));
            Clipboard.SetText(jsonText);
        }

        public void CopyPoseInRangeToClipboard()
        {
            if(CopyMoreThanSingleFrame.Value)
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
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();
            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && true)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip, currentFrame, 1)).Build();
                    command.PasteIntoSelectedBonesInRanges();
                    _commandExecutor.ExecuteCommand(command);

                    if (IncrementFrameAfterCopyOperation.Value)
                    {
                        _rider.Player.CurrentFrame++;
                    }
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        public void PasteMultipleFramesClipboardInRanges()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int.TryParse(FrameNrStart, out var insertAtFrameNr);
            int.TryParse(FrameNrEnd, out var pastedFramesLength);

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var maxFrame = _rider.AnimationClip.DynamicFrames.Count;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();

            if (insertAtFrameNr < 0)
            {
                MessageBox.Show("frame nr insert at is invalid (negatif number)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (insertAtFrameNr > maxFrame)
            {
                MessageBox.Show("frame nr insert at is invalid (bigger than total frames)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && !DontWarnDifferentSkeletons.Value)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;
                }

                var framesCount = parsedClipboardFrame.Frames.Keys.Count;
                if (framesCount > pastedFramesLength)
                {
                    var result = MessageBox.Show($"it is too long {pastedFramesLength} frames, the animation frames length is {framesCount}.", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var willCreateNewFrame = maxFrame < pastedFramesLength;
                var confirm = MessageBox.Show($"animation skeleton ${skeleton.SkeletonName}\n" +
                                              $"paste at frame {insertAtFrameNr}\n" +
                                              $"total frame length to paste {pastedFramesLength}\n" +
                                              $"{ ((willCreateNewFrame) ? $"this will extend the animation by {framesCount - maxFrame} frames\n" : "\n")  }" +
                                              $"continue?", "confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip, insertAtFrameNr, pastedFramesLength)).Build();
                command.PasteInRanges();
                _commandExecutor.ExecuteCommand(command);

                if (IncrementFrameAfterCopyOperation.Value)
                {
                    _rider.Player.CurrentFrame++;
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void PasteMultipleFramesClipboardWhole()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();
            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && true)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip)).Build();
                    command.PasteIntoSelectedBones();
                    _commandExecutor.ExecuteCommand(command);

                    if (IncrementFrameAfterCopyOperation.Value)
                    {
                        _rider.Player.CurrentFrame++;
                    }
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        public void PastePoseInRangeFromClipboard()
        {
            if(PasteUsingFormBelow.Value)
            {
                PasteMultipleFramesClipboardInRanges();
            }
            else if (!PasteUsingFormBelow.Value && CopyMoreThanSingleFrame.Value)
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
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(_previousSelectedBones == null || _previousSelectedBones.Count == 0)
            {
                MessageBox.Show("no bones were selected!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();
            var maxFrame = _rider.Player.AnimationClip.DynamicFrames.Count;
            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if(parsedClipboardFrame == null)
                {
                    MessageBox.Show("no animation in the clipboard!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && !DontWarnDifferentSkeletons.Value)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    var pastedFramesLength = parsedClipboardFrame.Frames.Count;

                    var willCreateNewFrame = maxFrame < pastedFramesLength;
                    var confirm = MessageBox.Show($"animation skeleton ${skeleton.SkeletonName}\n" +
                                              $"paste at frame {0}\n" +
                                              $"total frame length to paste {pastedFramesLength}\n" +
                                              $"{((willCreateNewFrame) ? $"this will extend the animation by {pastedFramesLength - maxFrame} frames\n" : "\n")}" +
                                              $"continue?", "confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes) return;

                    var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip, 
                        currentFrame, pastedFramesLength, _previousSelectedBones,
                        true, PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
                    command.PasteIntoSelectedBones();
                    _commandExecutor.ExecuteCommand(command);

                    if (IncrementFrameAfterCopyOperation.Value)
                    {
                        _rider.Player.CurrentFrame++;
                    }
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        public void PasteIntoSelectedCurrentNodeFromClipboardInRange()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int.TryParse(FrameNrStart, out var insertAtFrameNr);
            int.TryParse(FrameNrEnd, out var pastedFramesLength);

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var maxFrame = _rider.AnimationClip.DynamicFrames.Count;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();

            if (insertAtFrameNr < 0)
            {
                MessageBox.Show("frame nr insert at is invalid (negatif number)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (insertAtFrameNr > maxFrame)
            {
                MessageBox.Show("frame nr insert at is invalid (bigger than total frames)", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && !DontWarnDifferentSkeletons.Value)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;
                }

                var framesCount = parsedClipboardFrame.Frames.Keys.Count;
                if (pastedFramesLength > framesCount )
                {
                    var result = MessageBox.Show($"it is too long {pastedFramesLength} frames, the animation frames length is {framesCount}.", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var willCreateNewFrame = maxFrame < pastedFramesLength;
                var confirm = MessageBox.Show($"animation skeleton ${skeleton.SkeletonName}\n" +
                                              $"paste at frame {insertAtFrameNr}\n" +
                                              $"total frame length to paste {pastedFramesLength}\n" +
                                              $"{((willCreateNewFrame) ? $"this will extend the animation by {framesCount - maxFrame} frames\n" : "\n")}" +
                                              $"continue?", "confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip, 
                    insertAtFrameNr, pastedFramesLength, _previousSelectedBones,
                    true, PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
                command.PasteInRanges();
                _commandExecutor.ExecuteCommand(command);

                if (IncrementFrameAfterCopyOperation.Value)
                {
                    _rider.Player.CurrentFrame++;
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        private void PasteIntoSelectedCurrentNodeFromClipboard()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.Pause();
            var currentFrame = _rider.Player.CurrentFrame;
            var skeleton = _rider.Skeleton;
            var frameInJsonFormat = Clipboard.GetText();
            try
            {
                var parsedClipboardFrame = JsonConvert.DeserializeObject<PasteTransformFromClipboardBoneCommand.BoneTransformClipboardData>(frameInJsonFormat);
                if ((parsedClipboardFrame.SkeletonName != skeleton.SkeletonName) && true)
                {
                    var result = MessageBox.Show($"the clipboard skeleton is {parsedClipboardFrame.SkeletonName} but the target skeleton (which you are editing right now) is {skeleton.SkeletonName}. this will cause something to break if both skeletons are radically different.", "warn", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    var command = _commandFactory.Create<PasteTransformFromClipboardBoneCommand>().Configure(x => x.Configure(skeleton, parsedClipboardFrame, _rider.AnimationClip, 
                        currentFrame, 1, _previousSelectedBones, 
                        true, PastePosition.Value, PasteRotation.Value, PasteScale.Value)).Build();
                    command.PasteIntoSelectedBonesInRanges();
                    _commandExecutor.ExecuteCommand(command);

                    if (IncrementFrameAfterCopyOperation.Value)
                    {
                        _rider.Player.CurrentFrame++;
                    }
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("cannot parse the clipboard. it is not in asset editor frame format.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        public void PasteIntoInRangeSelectedCurrentNodeFromClipboard()
        {
            if (PasteUsingFormBelow.Value)
            {
                PasteIntoSelectedCurrentNodeFromClipboardInRange();
            }
            else if (!PasteUsingFormBelow.Value && CopyMoreThanSingleFrame.Value)
            {
                PasteIntoSelectedCurrentNodeFromClipboardWhole();
            }
            else
            {
                PasteIntoSelectedCurrentNodeFromClipboard();
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
        }

        public void FirstFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.CurrentFrame = 0;
        }

        public void PrevFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.CurrentFrame--;
        }
        public void NextFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.CurrentFrame++;
        }

        public void LastFrame()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rider.Player.CurrentFrame = _rider.AnimationClip.DynamicFrames.Count - 1;
        }

        public void SetStartFrameZero()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FrameNrStart = "0";
        }

        public void SetEndFrameMax()
        {
            if (_rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FrameNrEnd = (_rider.AnimationClip.DynamicFrames.Count - 1).ToString();
        }
    }
}
