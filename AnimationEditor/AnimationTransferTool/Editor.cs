using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.MathViews;
using CommonControls.Services;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;

namespace AnimationEditor.AnimationTransferTool
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AssetViewModel _copyTo;
        AssetViewModel _copyFrom;
        public AssetViewModel Generated { get; set; }
        List<IndexRemapping> _remappingInformaton;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public ObservableCollection<SkeletonBoneNode> FlatBoneList { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public AnimationSettings AnimationSettings { get; set; } = new AnimationSettings();

        SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); HightlightSelectedBones(value); }
        }

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel copyToAsset, AssetViewModel copyFromAsset, AssetViewModel generated, IComponentManager componentManager)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            Generated = generated;

            _copyFrom.SkeletonChanged += CopyFromSkeletonChanged;
            _copyTo.MeshChanged += CopyToMeshChanged;

            _copyTo.Offset = Matrix.CreateTranslation(new Vector3(0, 0, -2));
            _copyFrom.Offset = Matrix.CreateTranslation(new Vector3(0, 0, 2));

            AnimationSettings.OffsetGenerated.OnValueChanged += (vector) => generated.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));
            AnimationSettings.OffsetTarget.OnValueChanged += (vector) => _copyTo.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));
            AnimationSettings.OffsetSource.OnValueChanged += (vector) => _copyFrom.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));

            if(_copyTo.Skeleton != null)
                CopyToMeshChanged(_copyTo);
        }

        void HightlightSelectedBones(SkeletonBoneNode bone)
        {
            if (bone == null)
            {
                Generated.SelectedBoneIndex(-1);
                _copyFrom.SelectedBoneIndex(-1);
            }
            else
            {
                Generated.SelectedBoneIndex(bone.BoneIndex.Value);
                if (_remappingInformaton != null)
                { 
                    var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                    if (mapping != null)
                        _copyFrom.SelectedBoneIndex(mapping.NewValue);
                }
            }
        }

        private void CopyToMeshChanged(AssetViewModel newValue)
        {
            Generated.CopyMeshFromOther(newValue, true);
            CreateBoneOverview(newValue.Skeleton);
            HightlightSelectedBones(null);
        }

        private void CopyFromSkeletonChanged(GameSkeleton newValue)
        {
            _remappingInformaton = null;
            CreateBoneOverview(_copyTo.Skeleton);
            HightlightSelectedBones(null);
        }

        public void OpenMappingWindow()
        {
            var targetSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyTo.SkeletonName);
            var sourceSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.SkeletonName);

            var config = new RemappedAnimatedBoneConfiguration();
            config.MeshSkeletonName = _copyTo.SkeletonName;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeleton);

            config.ParnetModelSkeletonName = _copyFrom.SkeletonName;
            config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(sourceSkeleton);

            var window = new BoneMappingWindow(new BoneMappingViewModel(config));
            if (window.ShowDialog() == true)
            {
                _remappingInformaton = AnimatedBoneHelper.BuildRemappingList(config.MeshBones.First());
                UpdateAnimation();
                UpdateBonesAfterMapping(Bones);
            }
        }

        void UpdateBonesAfterMapping(IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                bone.HasMapping.Value = mapping != null;
                UpdateBonesAfterMapping(bone.Children);
            }
        }

        public void ClearRelativeSelectedBone()
        {
            if(SelectedBone != null)
                SelectedBone.SelectedRelativeBone = null;
        }

        public void UpdateAnimation()
        {
            if (_remappingInformaton == null)
            {
                MessageBox.Show("No mapping created?", "Error", MessageBoxButton.OK);
                return;
            }

            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return;
            }

            if (_copyFrom.AnimationClip == null)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return;
            }

            var service = new AnimationRemapperService(AnimationSettings, _remappingInformaton, Bones);
            var clip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, _copyFrom.AnimationClip);
            Generated.SetAnimationClip(clip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
        }

        public void SaveAnimation()
        {
            if (Generated.AnimationClip == null || Generated.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("No animation generated", "Error", MessageBoxButton.OK);
                return;
            }

            var orgName = _copyFrom.AnimationName.AnimationFile;
            var orgSkeleton = _copyFrom.Skeleton.SkeletonName;
            var newSkeleton = Generated.Skeleton.SkeletonName;
            var newName = orgName.Replace(orgSkeleton, newSkeleton);

            var animFile = Generated.AnimationClip.ConvertToFileFormat(Generated.Skeleton);
            SaveHelper.Save(_pfs, newName, null, AnimationFile.GetBytes(animFile));
        }
        public void ClearAllSettings()
        {
            if(MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                CreateBoneOverview(_copyTo.Skeleton);
        }


        public void UseTargetAsSource()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                _copyFrom.CopyMeshFromOther(_copyTo, true);
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            FlatBoneList.Clear();
            FlatBoneList.Add(null);

            if (skeleton == null)
                return;
            for (int i = 0; i<skeleton.BoneCount; i++)
            {
                SkeletonBoneNode newBone = null;
                var parentBoneId = skeleton.GetParentBone(i);
                if (parentBoneId == -1)
                {
                    newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, -1);
                    Bones.Add(newBone);
                }
                else
                {
                    var treeParent = BoneHelper.GetBoneFromId(Bones, parentBoneId);
                    if (treeParent != null)
                    {
                        newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, parentBoneId);
                        treeParent.Children.Add(newBone);
                    }
                }

                FlatBoneList.Add(newBone);
            }
        }
    }

    public class SkeletonBoneNode : NotifyPropertyChangedImpl
    {
        public NotifyAttr<int> BoneIndex { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> ParnetBoneIndex { get; set; } = new NotifyAttr<int>(-1);
        public NotifyAttr<string> BoneName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> HasMapping { get; set; } = new NotifyAttr<bool>(false);

        public SkeletonBoneNode(string boneName, int boneIndex, int parentBoneIndex)
        {
            BoneName.Value = boneName;
            BoneIndex.Value = boneIndex;
            ParnetBoneIndex.Value = parentBoneIndex;
        }

        public NotifyAttr<bool> IsLocalOffset { get; set; } = new NotifyAttr<bool>(false);
        public Vector3ViewModel RotationOffset { get; set; } = new Vector3ViewModel(0);
        public Vector3ViewModel TranslationOffset { get; set; } = new Vector3ViewModel(0);

        public NotifyAttr<bool> ForceSnapToWorld { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> FreezeTranslation { get; set; } = new NotifyAttr<bool>(false);

        public SkeletonBoneNode _selectedRelativeBone;
        public SkeletonBoneNode SelectedRelativeBone
        {
            get { return _selectedRelativeBone; }
            set { SetAndNotify(ref _selectedRelativeBone, value); }
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();

       
    }
}
