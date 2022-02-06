using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using View3D.Animation;
using View3D.Components.Rendering;

namespace AnimationEditor.AnimationBuilder
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _techSkeletonNode;
        IComponentManager _componentManager;
        CopyPasteManager _copyPasteManager;

        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> RefMeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SourceSkeletonName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<bool> ShowBonesAsWorldTransform { get; set; } = new NotifyAttr<bool>(true);

        public bool ShowSkeleton 
        {
            get => _techSkeletonNode.ShowSkeleton.Value;
            set { _techSkeletonNode.ShowSkeleton.Value = value; NotifyPropertyChanged();}
        }

        public bool ShowRefMesh
        {
            get => _techSkeletonNode.ShowMesh.Value;
            set { _techSkeletonNode.ShowMesh.Value = value; NotifyPropertyChanged(); }
        }

        public Vector3ViewModel SelectedBoneRotationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel SelectedBoneTranslationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public DoubleViewModel BoneVisualScale { get; set; } = new DoubleViewModel(1.5);

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();

        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get => _selectedBone;
            set  { SetAndNotify(ref _selectedBone, value); UpdateSelectedBoneValues(value); }
        }

        public string SelectedBoneName 
        {
            get => _selectedBone != null ? _selectedBone.BoneName : "";
            set {  UpdateSelectedBoneName(value); NotifyPropertyChanged();}
        }

        bool _hasTeckSkeletonTransform = false;
        public bool IsTechSkeleton
        {
            get => _hasTeckSkeletonTransform;
            set { SetTechSkeletonTransform(value); NotifyPropertyChanged(); }
        }

        public DoubleViewModel BoneScale { get; set; } = new DoubleViewModel(1);

        public Editor(PackFileService pfs, AssetViewModel techSkeletonNode, IComponentManager componentManager, CopyPasteManager copyPasteManager)
        {
            _pfs = pfs;
            _techSkeletonNode = techSkeletonNode;
            _componentManager = componentManager;
            _copyPasteManager = copyPasteManager;

            ShowBonesAsWorldTransform.PropertyChanged += (s, e) => UpdateSelectedBoneValues(_selectedBone);
            SelectedBoneTranslationOffset.OnValueChanged += HandleTranslationChanged;
            SelectedBoneRotationOffset.OnValueChanged += HandleTranslationChanged;
            BoneVisualScale.PropertyChanged += (s,e) =>_techSkeletonNode.SelectedBoneScale((float)BoneVisualScale.Value);
            BoneScale.PropertyChanged += HandleScaleChanged;
        }

        public void CreateEditor(string skeletonPath)
        {
            try
            {
                UpdateSelectedBoneValues(null);
                var packFile = _pfs.FindFile(skeletonPath);
                SkeletonName.Value = skeletonPath;
                _techSkeletonNode.SetSkeleton(packFile);
                RefreshBoneList();
                IsTechSkeleton = skeletonPath.ToLower().Contains("tech");
                SourceSkeletonName.Value = _techSkeletonNode.Skeleton.SkeletonName;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to load skeleton '{skeletonPath}'\n\n" + e.Message);
            }
        }

        void RefreshBoneList(int boneToSelect = -1)
        {
            Bones.Clear();
            if (_techSkeletonNode == null || _techSkeletonNode.Skeleton == null)
                return;

            var bones = SkeletonBoneNodeHelper.CreateBoneOverview(_techSkeletonNode.Skeleton);
            foreach (var bone in bones)
                Bones.Add(bone);

            if (boneToSelect >= 0 && boneToSelect < _techSkeletonNode.Skeleton.BoneCount)
                SelectedBone = Bones[boneToSelect];
        }

        void UpdateSelectedBoneValues(SkeletonBoneNode selectedBone)
        {
            SelectedBoneRotationOffset.DisableCallbacks = true;
            SelectedBoneTranslationOffset.DisableCallbacks = true;

            if (selectedBone == null)
            {
                SelectedBoneName = "";
                _techSkeletonNode.SelectedBoneIndex(-1);
                SelectedBoneRotationOffset.Clear();
                SelectedBoneTranslationOffset.Clear();
            }
            else
            {
                var boneIndex = selectedBone.BoneIndex;
                var position = _techSkeletonNode.Skeleton.Translation[boneIndex];
                var rotation = _techSkeletonNode.Skeleton.Rotation[boneIndex];
                if (ShowBonesAsWorldTransform.Value)
                {
                    var worldMatrix = _techSkeletonNode.Skeleton.GetWorldTransform(boneIndex);
                    worldMatrix.Decompose(out _, out rotation, out position);
                }

                var eulerRotation = MathUtil.QuaternionToEulerDegree(rotation);

                SelectedBoneName = _selectedBone.BoneName;
                _techSkeletonNode.SelectedBoneIndex(boneIndex);
                SelectedBoneRotationOffset.Set(eulerRotation);
                SelectedBoneTranslationOffset.Set(position);
            }

            SelectedBoneRotationOffset.DisableCallbacks = false;
            SelectedBoneTranslationOffset.DisableCallbacks = false;
        }

        void UpdateSelectedBoneName(string newName)
        {
            if (_selectedBone == null)
                return;

            _selectedBone.BoneName = newName;
            _techSkeletonNode.Skeleton.BoneNames[_selectedBone.BoneIndex] = newName;
        }

        private void SetTechSkeletonTransform(bool value)
        {
            _hasTeckSkeletonTransform = value;
            if (_hasTeckSkeletonTransform)
                _techSkeletonNode.SetTransform(Matrix.CreateScale(1, 1, -1));
            else
                _techSkeletonNode.SetTransform(Matrix.Identity);
        }

        private void HandleTranslationChanged(Vector3ViewModel newValue)
        { 
            if (_selectedBone == null)
                return;

            var boneIndex = _selectedBone.BoneIndex;

            var translationValue = SelectedBoneTranslationOffset.GetAsVector3();
            var quaternionValue = MathUtil.EulerDegreesToQuaternion(SelectedBoneRotationOffset.GetAsVector3());

            if (ShowBonesAsWorldTransform.Value)
            {
                var parentIndex = _techSkeletonNode.Skeleton.GetParentBoneIndex(boneIndex);
                if (parentIndex != -1)
                {
                    var parentTransform = _techSkeletonNode.Skeleton.GetWorldTransform(parentIndex);

                    var rotationWorld = MathUtil.EulerDegreesToQuaternion(SelectedBoneRotationOffset.GetAsVector3());
                    var translationWorld = SelectedBoneTranslationOffset.GetAsVector3();
                    var currentMatrixWorld = Matrix.CreateFromQuaternion(rotationWorld) * Matrix.CreateTranslation(translationWorld);
                    
                    var localSpaceMatrix = currentMatrixWorld * Matrix.Invert(parentTransform);
                    localSpaceMatrix.Decompose(out _, out quaternionValue, out translationValue);
                }
            }

            _techSkeletonNode.Skeleton.Translation[boneIndex] = translationValue;
            _techSkeletonNode.Skeleton.Rotation[boneIndex] = quaternionValue;
            _techSkeletonNode.Skeleton.RebuildSkeletonMatrix();
        }

        private void HandleScaleChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_selectedBone == null)
                return;

            var boneIndex = _selectedBone.BoneIndex;
            _techSkeletonNode.Skeleton.Scale[boneIndex] = (float)BoneScale.Value;
            _techSkeletonNode.Skeleton.RebuildSkeletonMatrix();
        }

        public void BakeSkeletonAction() => _techSkeletonNode.Skeleton.BakeScaleIntoSkeleton();
        

        public void FocusSelectedBoneAction()
        {
            if (_selectedBone == null)
                return;

            var camera = _componentManager.GetComponent<ArcBallCamera>();
            var worldPos = _techSkeletonNode.Skeleton.GetWorldTransform(_selectedBone.BoneIndex).Translation;
            camera.LookAt = worldPos;
        }

        public void CreateBoneAction()
        {
            if (_selectedBone == null)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(_selectedBone.BoneIndex);

            RefreshBoneList();
        }

        public void DuplicateBoneAction()
        {
            if (_selectedBone == null)
                return;

            // Create the bone
            var parentBoneIndex = _techSkeletonNode.Skeleton.GetParentBoneIndex(_selectedBone.BoneIndex);
            if (parentBoneIndex == -1)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(parentBoneIndex);

            // Copy data
            var copyIndex = _selectedBone.BoneIndex;
            var newBoneIndex = _techSkeletonNode.Skeleton.BoneCount - 1;
            _techSkeletonNode.Skeleton.BoneNames[newBoneIndex] = _techSkeletonNode.Skeleton.BoneNames[copyIndex] + "_cpy";
            _techSkeletonNode.Skeleton.Translation[newBoneIndex] = _techSkeletonNode.Skeleton.Translation[copyIndex];
            _techSkeletonNode.Skeleton.Rotation[newBoneIndex] = _techSkeletonNode.Skeleton.Rotation[copyIndex];
            _techSkeletonNode.Skeleton.RebuildSkeletonMatrix();

            RefreshBoneList();
        }

        public void DeleteBoneAction()
        {
            if (_selectedBone == null)
                return;

            _techSkeletonNode.Skeleton.DeleteBone(_selectedBone.BoneIndex);
            RefreshBoneList();
        }

    

        void PasteBones(GameSkeleton source, int sourceIndex, GameSkeleton target, int targetIndex, bool setUsingWorldTransform = false)
        {
            target.CreateChildBone(targetIndex);
            var newBoneIndex = target.BoneCount - 1;

            target.BoneNames[newBoneIndex] = source.BoneNames[sourceIndex];
            if (setUsingWorldTransform == false)
            {
                target.Translation[newBoneIndex] = source.Translation[sourceIndex];
                target.Rotation[newBoneIndex] = source.Rotation[sourceIndex];
            }
            else
            {
                var parentTransform = target.GetWorldTransform(targetIndex);
                var world = source.GetWorldTransform(sourceIndex);

                var localSpaceMatrix = world * Matrix.Invert(parentTransform);
                localSpaceMatrix.Decompose(out _, out var quaternionValue, out var translationValue);

                target.Translation[newBoneIndex] = Vector3.Zero;
                target.Rotation[newBoneIndex] = quaternionValue;
            }

            var sourceChildBones = source.GetDirectChildBones(sourceIndex);
            foreach (var childBone in sourceChildBones)
                PasteBones(source, childBone, target, newBoneIndex);
        }

        public void SaveSkeletonAction()
        {
            if (_techSkeletonNode.Skeleton == null)
                return;

            if (_techSkeletonNode.Skeleton.HasBoneScale())
            {
                MessageBox.Show("Skeleton has scale, this needs to be baked before the skeleton can be saved");
                return;
            }

            var skeletonClip = AnimationClip.CreateSkeletonAnimation(_techSkeletonNode.Skeleton);
            var animFile = skeletonClip.ConvertToFileFormat(_techSkeletonNode.Skeleton);
            animFile.Header.SkeletonName = SourceSkeletonName.Value;

            var result = SaveHelper.Save(_pfs, SkeletonName.Value, null, AnimationFile.ConvertToBytes(animFile));
            SkeletonName.Value = _pfs.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName.Value, ".bone_inv_trans_mats");
            SaveHelper.Save(_pfs, invMatrixPath, null, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeletonAction()
        {
            using (var browser = new PackFileBrowserWindow(_pfs))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".anim"});
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    var file = browser.SelectedFile;
                    var path = _pfs.GetFullPath(file);
                    CreateEditor(path);
                }
            }
        }

        public void LoadRefMeshAction()
        {
            using (var browser = new PackFileBrowserWindow(_pfs))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    var file = browser.SelectedFile;
                    _techSkeletonNode.SetMesh(file);
                    RefMeshName.Value = _pfs.GetFullPath(file);
                    CreateEditor(_techSkeletonNode.SkeletonName.Value);
                }
            }
        }
    }
}
