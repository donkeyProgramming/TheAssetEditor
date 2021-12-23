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

namespace AnimationEditor.TechSkeletonEditor
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _techSkeletonNode;
        IComponentManager _componentManager;

        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> ShowBonesAsWorldTransform { get; set; } = new NotifyAttr<bool>(true);

        public bool ShowSkeleton 
        {
            get => _techSkeletonNode.ShowSkeleton.Value;
            set { _techSkeletonNode.ShowSkeleton.Value = value; NotifyPropertyChanged();}
        }

        public Vector3ViewModel SelectedBoneRotationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel SelectedBoneTranslationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public DoubleViewModel BoneScale { get; set; } = new DoubleViewModel(1.5);

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
            set {  UpdateSelectedBoneValues(value); NotifyPropertyChanged();
            }
        }

        public Editor(PackFileService pfs, AssetViewModel techSkeletonNode, IComponentManager componentManager )
        {
            _pfs = pfs;
            _techSkeletonNode = techSkeletonNode;
            _componentManager = componentManager;

            ShowBonesAsWorldTransform.PropertyChanged += (s, e) => UpdateSelectedBoneValues(_selectedBone);
            SelectedBoneTranslationOffset.OnValueChanged += HandleTranslationChanged;
            SelectedBoneRotationOffset.OnValueChanged += HandleTranslationChanged;
            BoneScale.PropertyChanged += (s,e) =>_techSkeletonNode.SelectedBoneScale((float)BoneScale.Value);
        }

        public void CreateEditor(string skeletonPath)
        {
            try
            {
                UpdateSelectedBoneValues(null);
                var packFile = _pfs.FindFile(skeletonPath);
                var animationFile = AnimationFile.Create(packFile);
                var skeleton = new GameSkeleton(animationFile, null);

                var newBones = SkeletonBoneNodeHelper.CreateBoneOverview(skeleton);
                foreach (var bone in newBones)
                    Bones.Add(bone);

                SkeletonName.Value = skeletonPath;
                _techSkeletonNode.SetSkeleton(packFile);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to load skeleton '{skeletonPath}'\n\n" + e.Message);
            }
        }

        void UpdateSelectedBoneValues(SkeletonBoneNode selectedBone)
        {
            SelectedBoneRotationOffset.DisableCallbacks = true;
            SelectedBoneTranslationOffset.DisableCallbacks = true;

            if (selectedBone == null)
            {
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

                _techSkeletonNode.SelectedBoneIndex(boneIndex);
                SelectedBoneRotationOffset.Set(eulerRotation);
                SelectedBoneTranslationOffset.Set(position);

                //SelectedBoneRotationOffset.Set(GetSkeltonNodeRotation(selectedBone.BoneIndex, ShowBonesAsWorldTransform.Value));
                //SelectedBoneTranslationOffset.Set(GetSkeltonNodePosition(selectedBone.BoneIndex, ShowBonesAsWorldTransform.Value));
            }

            SelectedBoneRotationOffset.DisableCallbacks = false;
            SelectedBoneTranslationOffset.DisableCallbacks = false;
        }

        public void UpdateSelectedBoneName(string newName)
        {
            if (_selectedBone == null)
                return;
        }

        Vector3 GetSkeltonNodePosition(int index, bool world)
        {
            if (world == false)
                return _techSkeletonNode.Skeleton.Translation[index];

            var worldMatrix = _techSkeletonNode.Skeleton.GetWorldTransform(index);
            worldMatrix.Decompose(out _, out _, out var worldPosition);
            return worldPosition;
        }

        Vector3 GetSkeltonNodeRotation(int index, bool world)
        {
            if (world == false)
                return MathUtil.QuaternionToEulerDegree(_techSkeletonNode.Skeleton.Rotation[index]);
                
            var worldMatrix = _techSkeletonNode.Skeleton.GetWorldTransform(index);
            worldMatrix.Decompose(out _, out var quaternion, out _);
            return MathUtil.QuaternionToEulerDegree(quaternion);
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
                var parentIndex = _techSkeletonNode.Skeleton.GetParentBone(boneIndex);
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

        public void FocusSelectedBone()
        {
            if (_selectedBone == null)
                return;

            var camera = _componentManager.GetComponent<ArcBallCamera>();
            var worldPos = _techSkeletonNode.Skeleton.GetWorldTransform(_selectedBone.BoneIndex).Translation;
            camera.LookAt = worldPos;
        }

        public void CreateBone()
        {
            if (_selectedBone == null)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(_selectedBone.BoneIndex);

            Bones.Clear();
            var bones = SkeletonBoneNodeHelper.CreateBoneOverview(_techSkeletonNode.Skeleton);
            foreach (var bone in bones)
                Bones.Add(bone);
        }

        public void DeleteBone()
        {
            if (_selectedBone == null)
                return;

            _techSkeletonNode.Skeleton.DeleteBone(_selectedBone.BoneIndex);
        }

        public void SaveSkeleton()
        {
            if (_techSkeletonNode.Skeleton == null)
            {
                MessageBox.Show("No skeleton created.");
                return;
            }

            var skeletonClip = AnimationClip.CreateSkeletonAnimation(_techSkeletonNode.Skeleton);
            var animFile = skeletonClip.ConvertToFileFormat(_techSkeletonNode.Skeleton);
           
            var result = SaveHelper.Save(_pfs, SkeletonName.Value, null, AnimationFile.GetBytes(animFile));
            SkeletonName.Value = _pfs.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName.Value, ".bone_inv_trans_mats");
            SaveHelper.Save(_pfs, invMatrixPath, null, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeleton()
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
    }
}
