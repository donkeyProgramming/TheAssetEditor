using System.Collections.ObjectModel;

namespace CommonControls.Editors.BoneMapping
{
    public class RemappedAnimatedBoneConfiguration
    {
        public ObservableCollection<AnimatedBone> MeshBones { get; set; }
        public string MeshSkeletonName { get; set; }

        public ObservableCollection<AnimatedBone> ParentModelBones { get; set; }
       
        public string ParnetModelSkeletonName { get; set; }
    }
}
