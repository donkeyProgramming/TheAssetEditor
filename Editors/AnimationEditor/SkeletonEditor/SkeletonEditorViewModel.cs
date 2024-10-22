using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;

namespace AnimationEditor.SkeletonEditor
{
    public partial class SkeletonEditorViewModel : EditorHostBase, IFileEditor
    {
        SceneObject _techSkeletonNode;

        private readonly FocusSelectableObjectService _focusSelectableObjectService;
        private readonly PackFileService _packFileService;
        private readonly CopyPasteManager _copyPasteManager;
        private readonly SceneObjectEditor _sceneObjectEditor;

        [ObservableProperty] string _skeletonName = "";
        [ObservableProperty] string _refMeshName = "";
        [ObservableProperty] string _sourceSkeletonName = "";
        [ObservableProperty] bool _showBonesAsWorldTransform  = true;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _bones = new();
        [ObservableProperty] SkeletonBoneNode _selectedBone = null;
        [ObservableProperty] bool _isTechSkeleton = false;
        [ObservableProperty] float _boneVisualScale = 1.5f;
        [ObservableProperty] float _boneScale = 1;
        [ObservableProperty] Vector3ViewModel _selectedBoneRotationOffset;
        [ObservableProperty] Vector3ViewModel _selectedBoneTranslationOffset;

        public bool? ShowSkeleton
        {
            get => _techSkeletonNode?.ShowSkeleton.Value;
            set { _techSkeletonNode.ShowSkeleton.Value = value.Value; OnPropertyChanged(nameof(ShowSkeleton)); }
        }
       
        public bool? ShowRefMesh
        {
            get => _techSkeletonNode?.ShowMesh.Value;
            set { _techSkeletonNode.ShowMesh.Value = value.Value; OnPropertyChanged(nameof(ShowRefMesh)); }
        }

        public string SelectedBoneName
        {
            get => SelectedBone?.BoneName;
            set { UpdateSelectedBoneName(value); OnPropertyChanged(nameof(SelectedBoneName)); }
        }

        public override Type EditorViewModelType => typeof(EditorView);

        public SkeletonEditorViewModel(PackFileService pfs,
            CopyPasteManager copyPasteManager,
            SceneObjectEditor assetViewModelBuilder,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            FocusSelectableObjectService focusSelectableObjectService,
            IComponentInserter componentInserter,
            AnimationPlayerViewModel animationPlayerViewModel,
            IWpfGame wpfGame)
            :base(componentInserter, animationPlayerViewModel, wpfGame, focusSelectableObjectService)
        {
            DisplayName = "Skeleton Editor";

            _sceneObjectEditor = assetViewModelBuilder;
            _focusSelectableObjectService = focusSelectableObjectService;
            _packFileService = pfs;
            _copyPasteManager = copyPasteManager;
           
            _selectedBoneRotationOffset = new Vector3ViewModel(0, 0, 0, x=> HandleTranslationChanged());
            SelectedBoneTranslationOffset = new Vector3ViewModel(0, 0, 0, x => HandleTranslationChanged());

            var assetNode = sceneObjectViewModelBuilder.CreateAsset(false, "SkeletonNode", Color.Black, null);
            assetNode.IsControlVisible.Value = false;
            _techSkeletonNode = assetNode.Data;

            SceneObjects.Add(assetNode);
        }

        partial void OnShowBonesAsWorldTransformChanged(bool value) => RefreshBoneInformation(SelectedBone);
        partial void OnSelectedBoneChanged(SkeletonBoneNode value) => RefreshBoneInformation(value);
        partial void OnIsTechSkeletonChanged(bool value) => SetTechSkeletonTransform(value);
        partial void OnBoneVisualScaleChanged(float value) => _techSkeletonNode?.SelectedBoneScale(value);
        partial void OnBoneScaleChanged(float value) => BoneTransformHandler.Scale(SelectedBone, _techSkeletonNode.Skeleton, (float)BoneScale);

        
        public PackFile CurrentFile { get; private set; }
        public void LoadFile(PackFile file)
        {
            CurrentFile = file;
            var skeletonPath = _packFileService.GetFullPath(file);
            Create(_techSkeletonNode, skeletonPath);
        }

        void Create(SceneObject techSkeletonNode, string skeletonPath)
        {
            try
            {
                DisplayName = Path.GetFileName(skeletonPath);
                _techSkeletonNode = techSkeletonNode;
            
                RefreshBoneInformation(null);
                var packFile = _packFileService.FindFile(skeletonPath);
                SkeletonName = skeletonPath;
                _sceneObjectEditor.SetSkeleton(_techSkeletonNode, packFile);
                RefreshBoneList();
                IsTechSkeleton = skeletonPath.ToLower().Contains("tech");
                SourceSkeletonName = _techSkeletonNode.Skeleton.SkeletonName;
                
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
                SelectedBone = Bones[boneToSelect];
        }

        void RefreshBoneInformation(SkeletonBoneNode selectedBone)
        {
            SelectedBoneRotationOffset.DisableCallbacks = true;
            SelectedBoneTranslationOffset.DisableCallbacks = true;

            if (selectedBone == null)
            {
                SelectedBoneName = "";
                _techSkeletonNode?.SelectedBoneIndex(-1);
                SelectedBoneRotationOffset.Clear();
                SelectedBoneTranslationOffset.Clear();
            }
            else
            {
                var boneIndex = selectedBone.BoneIndex;
                var position = _techSkeletonNode.Skeleton.Translation[boneIndex];
                var rotation = _techSkeletonNode.Skeleton.Rotation[boneIndex];
                if (ShowBonesAsWorldTransform)
                {
                    var worldMatrix = _techSkeletonNode.Skeleton.GetWorldTransform(boneIndex);
                    worldMatrix.Decompose(out _, out rotation, out position);
                }

                var eulerRotation = MathUtil.QuaternionToEulerDegree(rotation);

                SelectedBoneName = SelectedBone.BoneName;
                _techSkeletonNode.SelectedBoneIndex(boneIndex);
                SelectedBoneRotationOffset.Set(eulerRotation);
                SelectedBoneTranslationOffset.Set(position);
            }

            SelectedBoneRotationOffset.DisableCallbacks = false;
            SelectedBoneTranslationOffset.DisableCallbacks = false;
        }

        void UpdateSelectedBoneName(string newName)
        {
            if (SelectedBone == null)
                return;

            SelectedBone.BoneName = newName;
            _techSkeletonNode.Skeleton.BoneNames[SelectedBone.BoneIndex] = newName;
        }

        private void SetTechSkeletonTransform(bool value)
        {
            if (value)
                _techSkeletonNode.SetTransform(Matrix.CreateScale(1, 1, -1));
            else
                _techSkeletonNode.SetTransform(Matrix.Identity);
        }

        private void HandleTranslationChanged()
        {
            BoneTransformHandler.Translate(SelectedBone,
                _techSkeletonNode.Skeleton, 
                SelectedBoneTranslationOffset.GetAsVector3(),
                SelectedBoneRotationOffset.GetAsVector3(),
                ShowBonesAsWorldTransform);
        }

        public void BakeSkeletonAction() => _techSkeletonNode.Skeleton.BakeScaleIntoSkeleton();

        public void FocusSelectedBoneAction()
        {
            if (SelectedBone == null)
                return;

            var worldPos = _techSkeletonNode.Skeleton.GetWorldTransform(SelectedBone.BoneIndex).Translation;
            _focusSelectableObjectService.LookAt(worldPos);
        }

        public void CreateBoneAction()
        {
            if (SelectedBone == null)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(SelectedBone.BoneIndex);
            RefreshBoneList();
        }

        public void DuplicateBoneAction()
        {
            BoneCopyPasteHandler.Duplicate(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void DeleteBoneAction()
        {
            BoneCopyPasteHandler.Delete(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void CopyBoneAction()
        {
            BoneCopyPasteHandler.Copy(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void PasteBoneAction()
        {
            BoneCopyPasteHandler.Paste(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
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
            animFile.Header.SkeletonName = SourceSkeletonName;
            var animationBytes = AnimationFile.ConvertToBytes(animFile);

            var result = SaveHelper.Save(_packFileService, SkeletonName, null, animationBytes);
            SkeletonName = _packFileService.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName, ".bone_inv_trans_mats");
            SaveHelper.Save(_packFileService, invMatrixPath, null, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeletonAction()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".anim"]);
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                var file = browser.SelectedFile;
                var path = _packFileService.GetFullPath(file);
                Create(_techSkeletonNode, path);
            }
        }

        public void LoadRefMeshAction()
        {
            using var browser = new PackFileBrowserWindow(_packFileService, [".variantmeshdefinition", ".wsmodel", ".rigid_model_v2"]);
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                var file = browser.SelectedFile;
                _sceneObjectEditor.SetMesh(_techSkeletonNode, file);
                RefMeshName = _packFileService.GetFullPath(file);
                Create(_techSkeletonNode, _techSkeletonNode.SkeletonName.Value);
            }
        }

    }
}
