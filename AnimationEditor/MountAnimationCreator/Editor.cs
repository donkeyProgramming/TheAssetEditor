using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
using CommonControls.ErrorListDialog;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CsvHelper;
using Filetypes.AnimationPack;
using Filetypes.RigidModel;
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


        bool _useSavePrefix= true;
        public bool UseSavePrefix
        {
            get { return _useSavePrefix; }
            set { SetAndNotify(ref _useSavePrefix, value); }
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

        SelectionManager _selectionManager;

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
            _mount.SetTransform(Matrix.CreateScale((float)AnimationSettings.Scale.Value));

            var newRiderAnim = GenerateMountAnimation(_mount.AnimationClip, _mount.Skeleton, _rider.AnimationClip, _rider.Skeleton, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.BoneIndex, SelectedRiderBone.ParentBoneIndex, AnimationSettings);

            // Apply
            NewAnimation.CopyMeshFromOther(_rider, true);
            NewAnimation.SetAnimationClip(newRiderAnim, new SkeletonAnimationLookUpHelper.AnimationReference("New mount animation", null));
            NewAnimation.IsSkeletonVisible = DisplayGeneratedRiderSkeleton;
            UpdateCanSaveAndPreviewStates();
        }


        static AnimationClip GenerateMountAnimation(AnimationClip mountAnimation, GameSkeleton mountSkeleton, AnimationClip riderAnimation, GameSkeleton riderSkeleton, 
            Rmv2MeshNode mountMesh, int mountVertexId, int riderBoneIndex, int parentBoneIndex, 
            AnimationSettingsViewModel animationSettings)
        {
            Vector3 translationOffset = new Vector3((float)animationSettings.Translation.X.Value, (float)animationSettings.Translation.Y.Value, (float)animationSettings.Translation.Z.Value);
            Vector3 rotationOffset = new Vector3((float)animationSettings.Rotation.X.Value, (float)animationSettings.Rotation.Y.Value, (float)animationSettings.Rotation.Z.Value);
            var rotationOffsetMatrix = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotationOffset.X), MathHelper.ToRadians(rotationOffset.Y), MathHelper.ToRadians(rotationOffset.Z));

            var newRiderAnim = riderAnimation.Clone();
            newRiderAnim.MergeStaticAndDynamicFrames();

            View3D.Animation.AnimationEditor.LoopAnimation(newRiderAnim, (int)animationSettings.LoopCounter.Value);

            // Resample
            if (animationSettings.FitAnimation)
                newRiderAnim = View3D.Animation.AnimationEditor.ReSample(riderSkeleton, newRiderAnim, mountAnimation.DynamicFrames.Count);
            newRiderAnim.StaticFrame = null;
            float mountScale = (float)animationSettings.Scale.Value;
            MeshAnimationHelper mountVertexPositionResolver = new MeshAnimationHelper(mountMesh, Matrix.CreateScale(mountScale));

            var maxFrameCount = Math.Min(mountAnimation.DynamicFrames.Count, newRiderAnim.DynamicFrames.Count);
            for (int i = 0; i < maxFrameCount; i++)
            {
                var mountFrame = AnimationSampler.Sample(i, 0, mountSkeleton, new List<AnimationClip> { mountAnimation });

                var mountBoneWorldMatrix = mountVertexPositionResolver.GetVertexTransformWorld(mountFrame, mountVertexId);
                mountBoneWorldMatrix.Decompose(out var _, out var mountVertexRot, out var mountVertexPos);

                // Make sure the rider moves along in the world with the same speed as the mount
                var mountMovement = mountFrame.BoneTransforms[0].Translation;
                newRiderAnim.DynamicFrames[i].Position[0] = mountAnimation.DynamicFrames[i].Position[0];
                newRiderAnim.DynamicFrames[i].Rotation[0] = Quaternion.Identity;

                //newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] = Vector3.Zero;
                //newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] = Quaternion.Identity;
                //
                //continue;
                // continue;
                // Keep the original rotation of the rider animation
                var origianlRotation = Quaternion.Identity;
                if (animationSettings.KeepRiderRotation)
                {
                    var riderFrame = AnimationSampler.Sample(i, 0, riderSkeleton, new List<AnimationClip> { riderAnimation });
                    var riderBoneWorldmatrix = riderFrame.GetSkeletonAnimatedWorld(riderSkeleton, riderBoneIndex);
                    riderBoneWorldmatrix.Decompose(out var _, out origianlRotation, out var _);

                    var axisAngles = MathUtil.ToAxisAngleDegrees(origianlRotation);

                    if (animationSettings.MaxRiderRotation.X.Value != -1)
                        origianlRotation.X = MathHelper.Clamp(axisAngles.X, (float)-animationSettings.MaxRiderRotation.X.Value, (float)animationSettings.MaxRiderRotation.X.Value);
                    if (animationSettings.MaxRiderRotation.Y.Value != -1)
                        origianlRotation.Y = MathHelper.Clamp(axisAngles.Y, (float)-animationSettings.MaxRiderRotation.Y.Value, (float)animationSettings.MaxRiderRotation.Y.Value);
                    if (animationSettings.MaxRiderRotation.Z.Value != -1)
                        origianlRotation.Z = MathHelper.Clamp(axisAngles.Z, (float)-animationSettings.MaxRiderRotation.Z.Value, (float)animationSettings.MaxRiderRotation.Z.Value);
                }

                var originalPosition = newRiderAnim.DynamicFrames[i].Position[riderBoneIndex];
                var originalRotation = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex];

                var newRiderPosition = mountVertexPos + translationOffset - mountFrame.BoneTransforms[0].Translation;
                var newRiderRotation = Quaternion.Multiply(Quaternion.Multiply(mountVertexRot, origianlRotation), rotationOffsetMatrix);

                var riderPositionDiff= newRiderPosition - originalPosition;
                var riderRotationDiff = newRiderRotation * Quaternion.Inverse(originalRotation);

                newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] = newRiderPosition;
                newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] = newRiderRotation;

                // Find all the bones at the same level (normally attachmentpoints) and move them as well
                if (parentBoneIndex != -1)
                {
                    var childNodes = riderSkeleton.GetChildBones(parentBoneIndex);

                    for (int boneId = 0; boneId < childNodes.Count; boneId++)
                    {
                        var id = childNodes[boneId];
                        if (id == riderBoneIndex)
                            continue;
                        newRiderAnim.DynamicFrames[i].Position[id] += riderPositionDiff;
                        newRiderAnim.DynamicFrames[i].Rotation[id] = riderRotationDiff * newRiderAnim.DynamicFrames[i].Rotation[id];
                    }
                }
            }

            return newRiderAnim;
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
            SaveAnimation(_rider.AnimationName.AnimationFile, NewAnimation.AnimationClip, NewAnimation.Skeleton);
        }

        public void AddAnimationToFragment()
        { }

        void SaveAnimation(string riderAnimationName, AnimationClip clip, GameSkeleton skeleton)
        {
            var animFile = clip.ConvertToFileFormat(skeleton);
            var bytes = AnimationFile.GetBytes(animFile);

            string savePath = "";
            if (UseSavePrefix)
                savePath = Path.GetDirectoryName(riderAnimationName) + "\\" + SavePrefixText + Path.GetFileName(riderAnimationName);
            else
            {
                using (var browser = new SavePackFileWindow(_pfs))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".anim" });
                    if (browser.ShowDialog() == true)
                    {
                        savePath = browser.FilePath;
                        if (savePath.Contains(".anim", StringComparison.InvariantCultureIgnoreCase) == false)
                            savePath += ".anim";
                    }
                    else
                        return;
                }
            }
            SaveHelper.Save(_pfs, savePath, null, bytes);
        }

        public void BatchProcess()
        {
            // Bin and animpack
            // Fragment
            // Animations

            // Rider fragment
            // Mount Fragment
            // New fragment

            var res = BatchProcessOptionsWindow.ShowDialog("MyFragment.frg");
            return;

            var inforResult = new List<ErrorListDataItem>();
            var mountSlots = MountLinkController.GetAllMountFragments();

            foreach (var mountFragment in mountSlots)
            {
                var riderFragment = MountLinkController.GetRiderFragmentFromMount(mountFragment);
                if (riderFragment == null)
                {
                    var expectedRiderSlot = "RIDER_" + mountFragment.Slot.Value;
                    inforResult.Add(ErrorListDataItem.Error(mountFragment.Slot.ToString(), "Animation (" + expectedRiderSlot + ") missing in rider fragment. Mount Anim = " + mountFragment.AnimationFile));
                    continue;
                }

                try
                {
                    var mountAnimPackFile = _pfs.FindFile(mountFragment.AnimationFile) as PackFile;
                    var mountAnim = new AnimationClip(AnimationFile.Create(mountAnimPackFile));

                    var riderAnimPackFile = _pfs.FindFile(riderFragment.AnimationFile) as PackFile;
                    var riderAnim = new AnimationClip(AnimationFile.Create(riderAnimPackFile));

                    var newRiderAnim = GenerateMountAnimation(mountAnim, _mount.Skeleton, riderAnim, _rider.Skeleton, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.BoneIndex, SelectedRiderBone.ParentBoneIndex, AnimationSettings);
                    SaveAnimation(riderFragment.AnimationFile, newRiderAnim, _rider.Skeleton);

                    var riderInfo = $"Rider:{riderFragment.Slot.Value}, {riderFragment.AnimationFile}";
                    var mountInfo = $"Mount:{mountFragment.Slot.Value}, {mountFragment.AnimationFile}";
                    inforResult.Add(ErrorListDataItem.Ok(mountFragment.Slot.ToString(), $"{mountInfo} | {riderInfo}"));
                }
                catch
                {
                    var riderInfo = $"RiderInfo:RIDER_{mountFragment.Slot.Value}, {riderFragment.AnimationFile}";
                    inforResult.Add(ErrorListDataItem.Error(mountFragment.Slot.ToString(), "Error generating rider animation:" + riderInfo));
                }
            }

            ErrorListWindow.ShowDialog("Combine Errors", inforResult.OrderBy(x=>x.ErrorType).ToList());
        }


    }
}
