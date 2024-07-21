using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class SkeletonBoneNode : ObservableObject
    {
        [ObservableProperty] string _boneName;
        [ObservableProperty] int _boneIndex;
        [ObservableProperty] int _parentBoneIndex;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _children = [];

        public SkeletonBoneNode(int boneId, int parentBoneId, string boneName)
        {
            BoneIndex = boneId;
            BoneName = boneName;
            ParentBoneIndex = parentBoneId;
        }   
    }

}
