using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
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

        string _savePrefixText = "";
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

        public Editor(PackFileService pfs, AssetViewModel rider, AssetViewModel mount, AssetViewModel newAnimation, IComponentManager componentManager)
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

            MountLinkController = new MountLinkController(pfs, rider, mount);

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
            NewAnimation.SetAnimationClip(newRiderAnim, Path.GetFileName(_rider.AnimationName));
            UpdateCanSaveAndPreviewStates();
        }


        static AnimationClip GenerateMountAnimation(AnimationClip mountAnimation, GameSkeleton mountSkeleton, AnimationClip riderAnimation, GameSkeleton riderSkeleton, 
            Rmv2MeshNode mountMesh, int mountVertexId, int riderBoneIndex, int parentBoneIndex, 
            AnimationSettingsViewModel AnimationSettings)
        {
            bool fitAnimations = AnimationSettings.FitAnimation;
            int loopCounter = (int)AnimationSettings.LoopCounter.Value;
            float mountScale = (float)AnimationSettings.Scale.Value;
            Vector3 translationOffset = new Vector3((float)AnimationSettings.Translation.X.Value, (float)AnimationSettings.Translation.Y.Value, (float)AnimationSettings.Translation.Z.Value);
            Vector3 rotationOffset = new Vector3((float)AnimationSettings.Rotation.X.Value, (float)AnimationSettings.Rotation.Y.Value, (float)AnimationSettings.Rotation.Z.Value);

            var newRiderAnim = riderAnimation.Clone();
            newRiderAnim.MergeStaticAndDynamicFrames();

            View3D.Animation.AnimationEditor.LoopAnimation(newRiderAnim, loopCounter);

            // Resample
            if (fitAnimations)
                newRiderAnim = View3D.Animation.AnimationEditor.ReSample(riderSkeleton, newRiderAnim, mountAnimation.DynamicFrames.Count);

            MeshAnimationHelper mountVertexPositionResolver = new MeshAnimationHelper(mountMesh, Matrix.CreateScale(mountScale));

            var maxFrameCount = Math.Min(mountAnimation.DynamicFrames.Count, newRiderAnim.DynamicFrames.Count);
            bool keepOriginalRotation = true;
            for (int i = 0; i < maxFrameCount; i++)
            {
                var mountFrame = AnimationSampler.Sample(i, 0, mountSkeleton, new List<AnimationClip> { mountAnimation }, true, true);
                var riderFrame = AnimationSampler.Sample(i, 0, riderSkeleton, new List<AnimationClip> { riderAnimation }, true, true);

                var mountBoneWorldMatrix = mountVertexPositionResolver.GetVertexPosition(mountFrame, mountVertexId);

                mountBoneWorldMatrix.Decompose(out var _, out var mountVertexRot, out var mountVertexPos);
                //mountFrame.BoneTransforms[0].WorldTransformDecompose(out var _, out var mountFrameRot, out var mountFramePos);
                //mountVertexRot = Quaternion.Identity;



                var rotationOffsetQuat = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(rotationOffset.X), MathHelper.ToRadians(rotationOffset.Y), MathHelper.ToRadians(rotationOffset.Z));

                var mountMovement = mountFrame.BoneTransforms[0].Translation;
                newRiderAnim.DynamicFrames[i].Position[0] = mountMovement;
                newRiderAnim.DynamicFrames[i].Rotation[0] = Quaternion.Identity;

                var origianlRotation = Quaternion.Identity;
                if (keepOriginalRotation)
                {
                    //    origianlRotation = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex];

                    var worldM = riderFrame.GetSkeletonAnimatedWorld(riderSkeleton, riderBoneIndex);
                    worldM.Decompose(out var _, out var riderRot, out var RiderPos);

                    origianlRotation = riderRot;
                }

                //public Matrix GetAnimatedWorldTranform(int boneIndex)
                //{
                //    if (_frame != null)
                //        return _frame.GetSkeletonAnimatedWorld(this, boneIndex);
                //
                //    return GetWorldTransform(boneIndex); ;
                //}

                //origianlRotation = Quaternion.Identity;

                var orgPos = newRiderAnim.DynamicFrames[i].Position[riderBoneIndex];
                var orgOrt = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex];
                newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] = mountVertexPos + translationOffset - mountFrame.BoneTransforms[0].Translation;
                newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] = Quaternion.Multiply(Quaternion.Multiply(mountVertexRot, origianlRotation), rotationOffsetQuat);

                var diffPos = newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] - orgPos;
                var diffRot = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] * Quaternion.Inverse(orgOrt);

                // Find all the bones at the same level
                if (parentBoneIndex != -1)
                {
                    var childNodes = riderSkeleton.GetChildBones(parentBoneIndex);

                    for (int boneId = 0; boneId < childNodes.Count; boneId++)
                    {
                        var id = childNodes[boneId];
                        if (id == riderBoneIndex)
                            continue;
                        newRiderAnim.DynamicFrames[i].Position[id] += diffPos;
                        newRiderAnim.DynamicFrames[i].Rotation[id] = diffRot * newRiderAnim.DynamicFrames[i].Rotation[id];
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
            SaveAnimation(_rider.AnimationName, NewAnimation.AnimationClip, NewAnimation.Skeleton);
        }

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
            // Entry : For each mount item:
            // Slot(id), status (OK, ERROR, MISSING IN RIDER), IsMountAnim, mount animation name, new rider animation name

            var csvLog = new List<object>();
            var mountSlots = MountLinkController.GetAllMountFragments();

            foreach (var mountFragment in mountSlots)
            {
                var riderFragment = MountLinkController.GetRiderFragmentFromMount(mountFragment);
                if (riderFragment == null)
                {
                    csvLog.Add(new { Status = "MISSING IN RIDER", MountSlot = mountFragment.Slot.ToString(),  MountAnimation = mountFragment.AnimationFile, RiderSlot = "", RiderAnimation = "" });
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

                    csvLog.Add(new { Status = "OK", MountSlot = mountFragment.Slot.ToString(), MountAnimation = mountFragment.AnimationFile, RiderSlot = riderFragment.Slot.ToString(), RiderAnimation = riderFragment.AnimationFile });
                }
                catch
                {
                    csvLog.Add(new { Status = "ERROR", MountSlot = mountFragment.Slot.ToString(), MountAnimation = mountFragment.AnimationFile, RiderSlot = riderFragment.Slot.ToString(), RiderAnimation = riderFragment.AnimationFile});
                }
            }

            var fileName = $"C:\\temp\\{Path.GetFileNameWithoutExtension(MountLinkController.SeletedRider.DisplayName)}_log.csv";
            _logger.Here().Information("Batch export log can be found at - " + fileName);
            using var writer = new StreamWriter(fileName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(csvLog);
        }
    }


    class MeshAnimationHelper
    {
        Rmv2MeshNode _mesh;
        Matrix _worldTransform;
        public MeshAnimationHelper(Rmv2MeshNode mesh, Matrix worldTransform)
        {
            _mesh = mesh;
            _worldTransform = worldTransform;
        }

        public Matrix GetVertexPosition(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Rmv2Geometry;
            var vert = geo.GetVertexExtented(vertexId);
            var m =  GetAnimationVertex(frame, vertexId);
            Matrix finalTransfrom = Matrix.CreateTranslation(new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z)) * m;
            return finalTransfrom;
        }


        Matrix GetAnimationVertex(AnimationFrame frame, int vertexId)
        {
            var geo = _mesh.Geometry as Rmv2Geometry;
            var vert = geo.GetVertexExtented(vertexId);

            var transformSum = Matrix.Identity;
            if (geo.WeightCount == 4)
            {
                int b0 = (int)vert.BlendIndices.X;
                int b1 = (int)vert.BlendIndices.Y;
                int b2 = (int)vert.BlendIndices.Z;
                int b3 = (int)vert.BlendIndices.W;

                float w1 = vert.BlendWeights.X;
                float w2 = vert.BlendWeights.Y;
                float w3 = vert.BlendWeights.Z;
                float w4 = vert.BlendWeights.W;

                Matrix m1 = frame.BoneTransforms[b0].WorldTransform;
                Matrix m2 = frame.BoneTransforms[b1].WorldTransform;
                Matrix m3 = frame.BoneTransforms[b2].WorldTransform;
                Matrix m4 = frame.BoneTransforms[b3].WorldTransform;
                transformSum.M11 = (m1.M11 * w1) + (m2.M11 * w2) + (m3.M11 * w3) + (m4.M11 * w4);
                transformSum.M12 = (m1.M12 * w1) + (m2.M12 * w2) + (m3.M12 * w3) + (m4.M12 * w4);
                transformSum.M13 = (m1.M13 * w1) + (m2.M13 * w2) + (m3.M13 * w3) + (m4.M13 * w4);
                transformSum.M21 = (m1.M21 * w1) + (m2.M21 * w2) + (m3.M21 * w3) + (m4.M21 * w4);
                transformSum.M22 = (m1.M22 * w1) + (m2.M22 * w2) + (m3.M22 * w3) + (m4.M22 * w4);
                transformSum.M23 = (m1.M23 * w1) + (m2.M23 * w2) + (m3.M23 * w3) + (m4.M23 * w4);
                transformSum.M31 = (m1.M31 * w1) + (m2.M31 * w2) + (m3.M31 * w3) + (m4.M31 * w4);
                transformSum.M32 = (m1.M32 * w1) + (m2.M32 * w2) + (m3.M32 * w3) + (m4.M32 * w4);
                transformSum.M33 = (m1.M33 * w1) + (m2.M33 * w2) + (m3.M33 * w3) + (m4.M33 * w4);
                transformSum.M41 = (m1.M41 * w1) + (m2.M41 * w2) + (m3.M41 * w3) + (m4.M41 * w4);
                transformSum.M42 = (m1.M42 * w1) + (m2.M42 * w2) + (m3.M42 * w3) + (m4.M42 * w4);
                transformSum.M43 = (m1.M43 * w1) + (m2.M43 * w2) + (m3.M43 * w3) + (m4.M43 * w4);
            }

            if (geo.WeightCount == 2)
            {
                int b0 = (int)vert.BlendIndices.X;
                int b1 = (int)vert.BlendIndices.Y;
                int b2 = (int)vert.BlendIndices.Z;
                int b3 = (int)vert.BlendIndices.W;

                float w1 = vert.BlendWeights.X;
                float w2 = vert.BlendWeights.Y;
                float w3 = vert.BlendWeights.Z;
                float w4 = vert.BlendWeights.W;

                Matrix m1 = frame.BoneTransforms[b0].WorldTransform;
                Matrix m2 = frame.BoneTransforms[b1].WorldTransform;
                Matrix m3 = frame.BoneTransforms[b2].WorldTransform;
                Matrix m4 = frame.BoneTransforms[b3].WorldTransform;

                transformSum.M11 = (m1.M11 * w1);
                transformSum.M12 = (m1.M12 * w1);
                transformSum.M13 = (m1.M13 * w1);
                transformSum.M21 = (m1.M21 * w1);
                transformSum.M22 = (m1.M22 * w1);
                transformSum.M23 = (m1.M23 * w1);
                transformSum.M31 = (m1.M31 * w1);
                transformSum.M32 = (m1.M32 * w1);
                transformSum.M33 = (m1.M33 * w1);
                transformSum.M41 = (m1.M41 * w1);
                transformSum.M42 = (m1.M42 * w1);
                transformSum.M43 = (m1.M43 * w1);
                
            }
            return transformSum * _worldTransform;
        }
        

    }
}
