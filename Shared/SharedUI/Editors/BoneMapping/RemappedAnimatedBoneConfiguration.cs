using System;
using System.Collections.ObjectModel;

namespace Shared.Ui.Editors.BoneMapping
{
    [Serializable]
    public class RemappedAnimatedBoneConfiguration
    {
        public ObservableCollection<AnimatedBone> MeshBones { get; set; }
        public string MeshSkeletonName { get; set; }

        public ObservableCollection<AnimatedBone> ParentModelBones { get; set; }

        public string ParnetModelSkeletonName { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public ISkeletonBoneHighlighter SkeletonBoneHighlighter { get; set; }
    }

    public interface ISkeletonBoneHighlighter
    {
        public void SelectTargetSkeletonBone(int index);
        public void SelectSourceSkeletonBone(int index);
    }
}
