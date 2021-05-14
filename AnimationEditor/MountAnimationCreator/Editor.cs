using AnimationEditor.Common.AnimationSettings;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator;
using Common;
using CommonControls.Common;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        string _selectedVertexesText;
        public string SelectedVertexesText
        {
            get { return _selectedVertexesText; }
            set { SetAndNotify(ref _selectedVertexesText, value); }
        }

        DoubleViewModel _mountScale = new DoubleViewModel(1);
        public DoubleViewModel MountScale
        {
            get { return _mountScale; }
            set { SetAndNotify(ref _mountScale, value); }
        }


        bool _useSavePrefix= true;
        public bool UseSavePrefix
        {
            get { return _useSavePrefix; }
            set { SetAndNotify(ref _useSavePrefix, value); }
        }

        string _savePrefixText = "new_prefix_";
        public string SavePrefixText
        {
            get { return _savePrefixText; }
            set { SetAndNotify(ref _savePrefixText, value); }
        }  

        AssetViewModel _newAnimation;
        public AssetViewModel NewAnimation { get => _newAnimation; set => SetAndNotify(ref _newAnimation, value); }

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();


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

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);

            _selectionManager = componentManager.GetComponent<SelectionManager>();
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
            CanPreview = SelectedRiderBone != null && _mountVertexes.Count != 0 && _mount != null && _rider != null;
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
            var mountAnim = _mount.AnimationClip;
            var newRiderAnim = _rider.AnimationClip.Clone();
            newRiderAnim.MergeStaticAndDynamicFrames();

            // Loop
            var loopCounter = AnimationSettings.LoopCounter.Value;
            View3D.Animation.AnimationEditor.LoopAnimation(newRiderAnim, (int)loopCounter);

            // Resample
            if (AnimationSettings.FitAnimation)
                newRiderAnim = View3D.Animation.AnimationEditor.ReSample(_rider.Skeleton, newRiderAnim, mountAnim.DynamicFrames.Count);

            var riderBoneIndex = SelectedRiderBone.BoneIndex;

            MeshAnimationHelper mountVertexPositionResolver = new MeshAnimationHelper(_mountVertexOwner);

            var maxFrameCount = Math.Min(_mount.AnimationClip.DynamicFrames.Count, newRiderAnim.DynamicFrames.Count);
            bool keepOriginalRotation = true;
            for (int i = 0; i < maxFrameCount; i++)
            {
                
                var mountFrame = AnimationSampler.Sample(i, 0, _mount.Skeleton, new List<AnimationClip> { _mount.AnimationClip }, true, true);

                var mountBoneWorldMatrix = mountVertexPositionResolver.GetVertexPosition(mountFrame, _mountVertexes.First());

                mountBoneWorldMatrix.Decompose(out var _, out var rot, out var pos);

                var rotationOffset =  Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians((float)AnimationSettings.Rotation.X.Value), MathHelper.ToRadians((float)AnimationSettings.Rotation.Y.Value), MathHelper.ToRadians((float)AnimationSettings.Rotation.Z.Value));

                var mountMovement = mountFrame.BoneTransforms[0].Translation;
                newRiderAnim.DynamicFrames[i].Position[0] = mountMovement;
                newRiderAnim.DynamicFrames[i].Rotation[0] = Quaternion.Identity;

                var origianlRotation = Quaternion.Identity;
                if(keepOriginalRotation)
                    origianlRotation = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex];


                var orgPos = newRiderAnim.DynamicFrames[i].Position[riderBoneIndex];
                var orgOrt = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex];
                newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] =  pos + new Vector3((float)AnimationSettings.Translation.X.Value, (float)AnimationSettings.Translation.Y.Value, (float)AnimationSettings.Translation.Z.Value) - mountFrame.BoneTransforms[0].Translation;
                newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] = Quaternion.Multiply(Quaternion.Multiply(rot, origianlRotation), rotationOffset);

                var diffPos = newRiderAnim.DynamicFrames[i].Position[riderBoneIndex] - orgPos;
                var diffRot = newRiderAnim.DynamicFrames[i].Rotation[riderBoneIndex] * Quaternion.Inverse( orgOrt);

                // Find all the bones at the same level
                var parentBoneIndex = SelectedRiderBone.ParentBoneIndex;
                if (parentBoneIndex != -1)
                {
                    var childNodes = _rider.Skeleton.GetChildBones(parentBoneIndex);

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

            // Apply
            NewAnimation.CopyMeshFromOther(_rider, true);
            NewAnimation.SetAnimationClip(newRiderAnim, Path.GetFileName(_rider.AnimationName));
            UpdateCanSaveAndPreviewStates();
        }

        public void SaveAnimation()
        {
            var animFile = NewAnimation.AnimationClip.ConvertToFileFormat(NewAnimation.Skeleton);
            var bytes = AnimationFile.GetBytes(animFile);

            string savePath = "";
            if (UseSavePrefix)
                savePath = Path.GetDirectoryName(_rider.AnimationName) + "\\" + SavePrefixText + Path.GetFileName(_rider.AnimationName);
            else
            {
                using (var browser = new SavePackFileWindow(_pfs))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".rigid_model_v2" });
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

        public void SaveAnimationAs()
        { 
        
        }
    }


    class MeshAnimationHelper
    {
        Rmv2MeshNode _mesh;
        public MeshAnimationHelper(Rmv2MeshNode mesh)
        {
            _mesh = mesh;
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
            return transformSum;
        }
        

    }
}
