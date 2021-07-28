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
using View3D.SceneNodes;
using View3D.Utility;

namespace AnimationEditor.AnimationTransferTool
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AssetViewModel _copyTo;
        AssetViewModel _copyFrom;
        AssetViewModel _generated;
        List<IndexRemapping> _remappingInformaton;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public ObservableCollection<SkeletonBoneNode> FlatBoneList { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        
        SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); OnBoneSelected(value); }
        }

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel copyToAsset, AssetViewModel copyFromAsset, AssetViewModel generated, IComponentManager componentManager)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            _generated = generated;

            _copyTo.SkeletonChanged += Target_SkeletonChanged;
            _copyFrom.SkeletonChanged += _source_SkeletonChanged;
            _copyTo.MeshChanged += MeshChanged;

            _copyTo.Offset = Matrix.CreateTranslation(new Vector3(0, 0, -1));
            _copyFrom.Offset = Matrix.CreateTranslation(new Vector3(0, 0, 1));

            MeshChanged(_copyTo);
        }

        void OnBoneSelected(SkeletonBoneNode bone)
        {
            if (bone == null)
            {
                _generated.SelectedBoneIndex(-1);
                _copyFrom.SelectedBoneIndex(-1);
            }
            else
            {
                _generated.SelectedBoneIndex(bone.BoneIndex.Value);
                if (_remappingInformaton != null)
                { 
                    var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                    if (mapping != null)
                        _copyFrom.SelectedBoneIndex(mapping.NewValue);
                }
            }
        }

        private void MeshChanged(AssetViewModel newValue)
        {
            _generated.CopyMeshFromOther(newValue, true);
            CreateBoneOverview(newValue.Skeleton);
            OnBoneSelected(null);
        }

        private void _source_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            //throw new NotImplementedException();
        }

        private void Target_SkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            //throw new NotImplementedException();
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

            if (config.MeshSkeletonName == config.ParnetModelSkeletonName)
                MessageBox.Show("The two models share skeleton, no need for mapping?", "Error", MessageBoxButton.OK);

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

        public void UpdateAnimation()
        {
            if (_remappingInformaton == null)
            {
                MessageBox.Show("No config?", "Error", MessageBoxButton.OK);
                return;
            }

            // Skeleton null, animatiom null
            // Relative bone list clear

            var service = new AnimationRemapperService(_remappingInformaton, Bones);
            AnimationClip clip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, _copyFrom.AnimationClip);
            _generated.SetAnimationClip(clip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
        }






            //---void 
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
        
        public SkeletonBoneNode _selectedRelativeBone;
        public SkeletonBoneNode SelectedRelativeBone
        {
            get { return _selectedRelativeBone; }
            set { SetAndNotify(ref _selectedRelativeBone, value); }
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
    }
}
