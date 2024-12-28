using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editor.VisualSkeletonEditor.SkeletonEditor
{
    public partial class SkeletonEditorViewModel : EditorHostBase, IFileEditor
    {
        SceneObject _techSkeletonNode;

        private readonly IPackFileService _packFileService;
        private readonly CopyPasteManager _copyPasteManager;
        private readonly IStandardDialogs _packFileUiProvider;
        private readonly IFileSaveService _packFileSaveService;

        [ObservableProperty] string _skeletonName = "";
        [ObservableProperty] string _refMeshName = "";
        [ObservableProperty] string _sourceSkeletonName = "";
        [ObservableProperty] bool _showBonesAsWorldTransform  = true;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _bones = new();
        [ObservableProperty] SkeletonBoneNode? _selectedBone = null;
        [ObservableProperty] bool _isTechSkeleton = false;
        [ObservableProperty] float _boneVisualScale = 1.5f;
        [ObservableProperty] float _boneScale = 1;
        [ObservableProperty] Vector3ViewModel _selectedBoneRotationOffset;
        [ObservableProperty] Vector3ViewModel _selectedBoneTranslationOffset;
        [ObservableProperty] string _selectedBoneName;
        [ObservableProperty] bool _showSkeleton = true;
        [ObservableProperty] bool _showRefMesh = true;

        public override Type EditorViewModelType => typeof(EditorView);

        public SkeletonEditorViewModel(
            IPackFileService pfs,
            CopyPasteManager copyPasteManager,
            IEditorHostParameters editorHostParameters,
            IStandardDialogs packFileUiProvider,
            IFileSaveService packFileSaveService)
            : base(editorHostParameters)
        {
            DisplayName = "Skeleton Editor";

            _packFileService = pfs;
            _copyPasteManager = copyPasteManager;
            _packFileUiProvider = packFileUiProvider;
            _packFileSaveService = packFileSaveService;
            _selectedBoneRotationOffset = new Vector3ViewModel(0, 0, 0, x=> HandleTranslationChanged());
            _selectedBoneTranslationOffset = new Vector3ViewModel(0, 0, 0, x => HandleTranslationChanged());

            Initialize();
        }

        void Initialize()
        {
            var assetNode = _sceneObjectViewModelBuilder.CreateAsset("Skeleton", false, "SkeletonNode", Color.Black, null);
            assetNode.IsControlVisible = false;
            _techSkeletonNode = assetNode.Data;

            SceneObjects.Add(assetNode);
        }

        partial void OnShowBonesAsWorldTransformChanged(bool value) => RefreshBoneInformation(SelectedBone);
        partial void OnSelectedBoneChanged(SkeletonBoneNode? value) => RefreshBoneInformation(value);
        partial void OnIsTechSkeletonChanged(bool value) => SetTechSkeletonTransform(value);
        partial void OnBoneVisualScaleChanged(float value) => _techSkeletonNode?.SelectedBoneScale(value);
        partial void OnBoneScaleChanged(float value) => BoneTransformHandler.Scale(SelectedBone, _techSkeletonNode.Skeleton, (float)BoneScale);
        partial void OnSelectedBoneNameChanged(string value) => UpdateSelectedBoneName(value);
        partial void OnShowSkeletonChanged(bool value) => _techSkeletonNode.ShowSkeleton.Value = value;
        partial void OnShowRefMeshChanged(bool value) => _techSkeletonNode.ShowMesh.Value = value;


        public PackFile CurrentFile { get; private set; }
        public void LoadFile(PackFile file)
        {
            CurrentFile = file;
            var skeletonPath = _packFileService.GetFullPath(file);
            LoadSkeleton(_techSkeletonNode, skeletonPath);
        }

        void LoadSkeleton(SceneObject techSkeletonNode, string skeletonPath)
        {
            try
            {
                DisplayName = Path.GetFileName(skeletonPath);
                _techSkeletonNode = techSkeletonNode;
            
                RefreshBoneInformation(null);
                var packFile = _packFileService.FindFile(skeletonPath);
                SkeletonName = skeletonPath;
                SceneObjectEditor.SetSkeleton(_techSkeletonNode, packFile);
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
                _techSkeletonNode.Offset = Matrix.CreateScale(1, 1, -1);
            else
                _techSkeletonNode.Offset = Matrix.Identity;
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
            FocusService.LookAt(worldPos);
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
            BoneManipulator.Duplicate(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void DeleteBoneAction()
        {
            BoneManipulator.Delete(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void CopyBoneAction()
        {
            BoneManipulator.Copy(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void PasteBoneAction()
        {
            BoneManipulator.Paste(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
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

            var result = _packFileSaveService.Save(SkeletonName, animationBytes, false);
            SkeletonName = _packFileService.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName, ".bone_inv_trans_mats");
            _packFileSaveService.Save(invMatrixPath, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeletonAction()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".anim"]);
            if(result.Result && result.File != null)
            {
                var path = _packFileService.GetFullPath(result.File);
                LoadSkeleton(_techSkeletonNode, path);
            }
        }

        public void LoadRefMeshAction()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".variantmeshdefinition", ".wsmodel", ".rigid_model_v2"]);
            if (result.Result && result.File != null)
            {
                var file = result.File;
                SceneObjectEditor.SetMesh(_techSkeletonNode, file, false);
                RefMeshName = _packFileService.GetFullPath(file);
            }
        }
    }
}
