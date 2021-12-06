using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.MathViews;
using CommonControls.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using View3D.Animation;

namespace AnimationEditor.TechSkeletonEditor
{
    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _techSkeletonNode;

        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SelectedNodeName { get; set; } = new NotifyAttr<string>("");

        public bool ShowSkeleton 
        {
            get => _techSkeletonNode.ShowSkeleton.Value;
            set 
            {
                _techSkeletonNode.ShowSkeleton.Value = value;
                NotifyPropertyChanged();
            }
        }

        public Vector3ViewModel SelectedBoneRotationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel SelectedBoneTranslationOffset { get; set; } = new Vector3ViewModel(0, 0, 0);

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();

        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get => _selectedBone;
            set  { SetAndNotify(ref _selectedBone, value); UpdateSelectedBone(value); }
        }

        public Editor(PackFileService pfs, AssetViewModel techSkeletonNode )
        {
            _pfs = pfs;
            _techSkeletonNode = techSkeletonNode;
        }

        public void Create(string path)
        {
            try
            {
                UpdateSelectedBone(null);
                var packFile = _pfs.FindFile(path);
                var animationFile = AnimationFile.Create(packFile);
                var skeleton = new GameSkeleton(animationFile, null);

                var newBones = SkeletonBoneNodeHelper.CreateBoneOverview(skeleton);
                foreach (var bone in newBones)
                    Bones.Add(bone);

                SkeletonName.Value = path;

                _techSkeletonNode.SetSkeleton(packFile);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load skeleton\n\n" + e.Message);
            }
        }

        void UpdateSelectedBone(SkeletonBoneNode selectedBone)
        {
            if (selectedBone == null)
            {
                SelectedNodeName.Value = "";
                _techSkeletonNode.SelectedBoneIndex(-1);

                SelectedBoneRotationOffset.Set(0, 0, 0);
                SelectedBoneTranslationOffset.Set(0, 0, 0);
            }
            else
            {
                SelectedNodeName.Value = selectedBone.BoneName + $"[{selectedBone.BoneIndex}]";
                _techSkeletonNode.SelectedBoneIndex(selectedBone.BoneIndex);

                SelectedBoneRotationOffset.Set(0, selectedBone.BoneIndex, 0);
                SelectedBoneTranslationOffset.Set(selectedBone.BoneIndex, 0, 0);
            }
        }

        // Load skeleton
        // Save skeleton
        // Apply updates to selected node
        // Create node
        // Delete node 

    }
}
