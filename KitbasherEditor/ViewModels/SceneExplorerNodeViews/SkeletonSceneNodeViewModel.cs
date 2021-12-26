using CommonControls.Common;
using CommonControls.MathViews;
using CommonControls.Services;
using System.Collections.ObjectModel;
using View3D.Animation;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class SkeletonSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        SkeletonNode _meshNode;
        public SkeletonSceneNodeViewModel(SkeletonNode node, PackFileService pf, SkeletonAnimationLookUpHelper animLookUp)
        {
            _meshNode = node;
            CreateBoneOverview(_meshNode.AnimationProvider.Skeleton);
            BoneScale.PropertyChanged += BoneScale_PropertyChanged;
        }

        private void BoneScale_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _meshNode.SkeletonScale = (float)BoneScale.Value;
        }

        int _boneCount = 0;
        public int BoneCount
        {
            get { return _boneCount; }
            set { SetAndNotify(ref _boneCount, value); }
        }

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();

        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set 
            { 
                SetAndNotify(ref _selectedBone, value);
                if (_selectedBone == null)
                    _meshNode.SelectedBoneIndex = null;
                else
                    _meshNode.SelectedBoneIndex = _selectedBone.BoneIndex;
              }
        }


        public DoubleViewModel BoneScale { get; set; } = new DoubleViewModel(1);


        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            BoneCount = 0;

            if (skeleton == null)
                return;

            BoneCount = skeleton.BoneCount;
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
                else
                {
                    var treeParent = GetParent(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
            }
        }

        SkeletonBoneNode CreateNode(int boneId, int parentBoneId, string boneName)
        {
            SkeletonBoneNode item = new SkeletonBoneNode
            {
                BoneIndex = boneId,
                BoneName = boneName,
                ParentBoneIndex = parentBoneId
            };
            return item;
        }

        SkeletonBoneNode GetParent(ObservableCollection<SkeletonBoneNode> root, int parentBoneId)
        {
            foreach (SkeletonBoneNode item in root)
            {
                if (item.BoneIndex == parentBoneId)
                    return item;

                var result = GetParent(item.Children, parentBoneId);
                if (result != null)
                    return result;
            }
            return null;
        }

        public void Dispose()
        {
            BoneScale.PropertyChanged -= BoneScale_PropertyChanged;
        }


        public class SkeletonBoneNode : NotifyPropertyChangedImpl
        {
            string _boneName;
            public string BoneName
            {
                get { return _boneName; }
                set { SetAndNotify(ref _boneName, value); }
            }

            int _boneIndex;
            public int BoneIndex
            {
                get { return _boneIndex; }
                set { SetAndNotify(ref _boneIndex, value); }
            }


            int _parentBoneIndex;
            public int ParentBoneIndex
            {
                get { return _parentBoneIndex; }
                set { SetAndNotify(ref _parentBoneIndex, value); }
            }

            public override string ToString()
            {
                return BoneName + "[" + BoneIndex + "]";
            }

            public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        }
    }


}
