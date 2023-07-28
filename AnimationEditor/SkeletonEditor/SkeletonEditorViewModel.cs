using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using View3D.Animation;
using View3D.Services;

namespace AnimationEditor.SkeletonEditor
{
    public class SkeletonEditorViewModel : NotifyPropertyChangedImpl, IHostedEditor<SkeletonEditorViewModel>
    {
        SceneObject _techSkeletonNode;

        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly FocusSelectableObjectService _focusSelectableObjectService;
        private readonly PackFileService _packFileService;
        private readonly CopyPasteManager _copyPasteManager;
        private readonly SceneObjectBuilder _assetViewModelBuilder;

        public string EditorName => "Skeleton Editor";


        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> RefMeshName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SourceSkeletonName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> ShowBonesAsWorldTransform { get; set; } = new NotifyAttr<bool>(true);

        public bool? ShowSkeleton
        {
            get => _techSkeletonNode?.ShowSkeleton.Value;
            set { _techSkeletonNode.ShowSkeleton.Value = value.Value; NotifyPropertyChanged(); }
        }
        
        public bool? ShowRefMesh
        {
            get => _techSkeletonNode?.ShowMesh.Value;
            set { _techSkeletonNode.ShowMesh.Value = value.Value; NotifyPropertyChanged(); }
        }

        public Vector3ViewModel SelectedBoneRotationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel SelectedBoneTranslationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public DoubleViewModel BoneVisualScale { get; set; } = new DoubleViewModel(1.5);
        public DoubleViewModel BoneScale { get; set; } = new DoubleViewModel(1);

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new();
        public NotifyAttr<SkeletonBoneNode> SelectedBone { get; set; } = new();
        public NotifyAttr<bool> IsTechSkeleton { get; set; } = new();

        public string SelectedBoneName
        {
            get => SelectedBone.Value?.BoneName;
            set { UpdateSelectedBoneName(value); NotifyPropertyChanged(); }
        }

        public SkeletonEditorViewModel(PackFileService pfs,
            CopyPasteManager copyPasteManager,
            SceneObjectBuilder assetViewModelBuilder,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            FocusSelectableObjectService focusSelectableObjectService)
        {
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _focusSelectableObjectService = focusSelectableObjectService;
            _packFileService = pfs;
            _copyPasteManager = copyPasteManager;
            _assetViewModelBuilder = assetViewModelBuilder;

            ShowBonesAsWorldTransform.PropertyChanged += (s, e) => RefreshBoneInformation(SelectedBone.Value);
            SelectedBoneTranslationOffset.OnValueChanged += HandleTranslationChanged;
            SelectedBoneRotationOffset.OnValueChanged += HandleTranslationChanged;
            BoneScale.PropertyChanged += HandleScaleChanged;

            SelectedBone.PropertyChanged += (x, y) => RefreshBoneInformation((x as NotifyAttr<SkeletonBoneNode>).Value);
            IsTechSkeleton.PropertyChanged += (x, y) => SetTechSkeletonTransform((x as NotifyAttr<bool>).Value);
        }

        public void Initialize(EditorHost<SkeletonEditorViewModel> owner)
        {
            var item = _sceneObjectViewModelBuilder.CreateAsset(false, "not_in_use1", Color.Black, null);
            item.IsControlVisible.Value = false;

            Create(item.Data, @"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\tech\nor_war_mammoth_howdah_01.anim");
            owner.SceneObjects.Add(item);
        }

        void Create(SceneObject techSkeletonNode, string skeletonPath)
        {
            try
            {
                _techSkeletonNode = techSkeletonNode;
                BoneVisualScale.PropertyChanged += (s, e) => _techSkeletonNode.SelectedBoneScale((float)BoneVisualScale.Value);

                RefreshBoneInformation(null);
                var packFile = _packFileService.FindFile(skeletonPath);
                SkeletonName.Value = skeletonPath;
                _assetViewModelBuilder.SetSkeleton(_techSkeletonNode, packFile);
                RefreshBoneList();
                IsTechSkeleton.Value = skeletonPath.ToLower().Contains("tech");
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
            if (_techSkeletonNode?.Skeleton == null)
                return;

            var bones = SkeletonBoneNodeHelper.CreateBoneOverview(_techSkeletonNode.Skeleton);
            foreach (var bone in bones)
                Bones.Add(bone);

            if (boneToSelect >= 0 && boneToSelect < _techSkeletonNode.Skeleton.BoneCount)
                SelectedBone.Value = Bones[boneToSelect];
        }

        void RefreshBoneInformation(SkeletonBoneNode selectedBone)
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

                SelectedBoneName = SelectedBone.Value.BoneName;
                _techSkeletonNode.SelectedBoneIndex(boneIndex);
                SelectedBoneRotationOffset.Set(eulerRotation);
                SelectedBoneTranslationOffset.Set(position);
            }

            SelectedBoneRotationOffset.DisableCallbacks = false;
            SelectedBoneTranslationOffset.DisableCallbacks = false;
        }

        void UpdateSelectedBoneName(string newName)
        {
            if (SelectedBone.Value == null)
                return;

            SelectedBone.Value.BoneName = newName;
            _techSkeletonNode.Skeleton.BoneNames[SelectedBone.Value.BoneIndex] = newName;
        }

        private void SetTechSkeletonTransform(bool value)
        {
            if (value)
                _techSkeletonNode.SetTransform(Matrix.CreateScale(1, 1, -1));
            else
                _techSkeletonNode.SetTransform(Matrix.Identity);
        }

        private void HandleTranslationChanged(Vector3ViewModel newValue)
        {
            BoneTransformHandler.Translate(SelectedBone.Value,
                _techSkeletonNode.Skeleton, 
                SelectedBoneTranslationOffset.GetAsVector3(),
                SelectedBoneRotationOffset.GetAsVector3(),
                ShowBonesAsWorldTransform.Value);
        }

        private void HandleScaleChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            BoneTransformHandler.Scale(SelectedBone.Value, _techSkeletonNode.Skeleton, (float)BoneScale.Value);
        }

        public void BakeSkeletonAction() => _techSkeletonNode.Skeleton.BakeScaleIntoSkeleton();

        public void FocusSelectedBoneAction()
        {
            if (SelectedBone.Value == null)
                return;

            var worldPos = _techSkeletonNode.Skeleton.GetWorldTransform(SelectedBone.Value.BoneIndex).Translation;
            _focusSelectableObjectService.LookAt(worldPos);
        }

        public void CreateBoneAction()
        {
            if (SelectedBone.Value == null)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(SelectedBone.Value.BoneIndex);
            RefreshBoneList();
        }

        public void DuplicateBoneAction()
        {
            BoneCopyPasteHandler.Duplicate(SelectedBone.Value, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void DeleteBoneAction()
        {
            BoneCopyPasteHandler.Delete(SelectedBone.Value, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void CopyBoneAction()
        {
            BoneCopyPasteHandler.Copy(_copyPasteManager, SelectedBone.Value, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void PasteBoneAction()
        {
            BoneCopyPasteHandler.Paste(_copyPasteManager, SelectedBone.Value, _techSkeletonNode.Skeleton);
            RefreshBoneList();
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
            var animationBytes = AnimationFile.ConvertToBytes(animFile);

            var result = SaveHelper.Save(_packFileService, SkeletonName.Value, null, animationBytes);
            SkeletonName.Value = _packFileService.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName.Value, ".bone_inv_trans_mats");
            SaveHelper.Save(_packFileService, invMatrixPath, null, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeletonAction()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, new[] { ".anim"});
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                var file = browser.SelectedFile;
                var path = _packFileService.GetFullPath(file);
                Create(_techSkeletonNode, path);
            }
        }

        public void LoadRefMeshAction()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, new[] { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                var file = browser.SelectedFile;
                _assetViewModelBuilder.SetMesh(_techSkeletonNode, file);
                RefMeshName.Value = _packFileService.GetFullPath(file);
                Create(_techSkeletonNode, _techSkeletonNode.SkeletonName.Value);
            }
        }
    }
}
