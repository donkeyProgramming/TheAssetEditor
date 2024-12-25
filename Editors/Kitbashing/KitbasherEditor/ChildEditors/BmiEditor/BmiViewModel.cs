using System.Collections.ObjectModel;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.SceneNodes;
using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.MathViews;

namespace KitbasherEditor.ViewModels.BmiEditor
{
    public class BmiViewModel : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        private readonly CommandFactory _commandFactory;
        GameSkeleton _skeleton;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); CheckButtonsEnabled = value != null; }
        }

        public bool _checkButtonsEnabled = false;
        public bool CheckButtonsEnabled
        {
            get { return _checkButtonsEnabled; }
            set { SetAndNotify(ref _checkButtonsEnabled, value); }
        }

        DoubleViewModel _scaleFactor = new DoubleViewModel(0.002);
        public DoubleViewModel ScaleFactor
        {
            get { return _scaleFactor; }
            set { SetAndNotify(ref _scaleFactor, value); }
        }

        public BmiViewModel(CommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public void Initialize(GameSkeleton skeleton, Rmv2MeshNode meshNode)
        {
            _meshNode = meshNode;
            _skeleton = skeleton;
            CreateBoneOverview(_skeleton);
        }

        public void CheckAllChildren()
        {
            SelectedBone.SetCheckStatusForSelfAndChildren(true);
        }

        public void UnCheckAllChildren()
        {
            SelectedBone.SetCheckStatusForSelfAndChildren(false);
        }

        public void Apply()
        {
            _commandFactory.Create<GrowMeshCommand>()
                .Configure(x => x.Configure(_skeleton, _meshNode, (float)_scaleFactor.Value, Bones.First().GetAllCheckedChildBoneIndexes()))
                .BuildAndExecute();
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            var boneIndexsUsed = _meshNode.Geometry.GetUniqeBlendIndices();

            Bones.Clear();

            if (skeleton == null)
                return;

            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i], boneIndexsUsed));
                }
                else
                {
                    var treeParent = GetParent(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i], boneIndexsUsed));
                }
            }

            Bones = FilterHelper.FilterBoneList("", true, Bones);
        }

        SkeletonBoneNode CreateNode(int boneId, int parentBoneId, string boneName, List<byte> usedBonesList)
        {
            SkeletonBoneNode item = new SkeletonBoneNode
            {
                BoneIndex = boneId,
                BoneName = boneName,
                ParentBoneIndex = parentBoneId,
                IsUsedByCurrentModel = usedBonesList.IndexOf((byte)boneId) != -1
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
    }
}
