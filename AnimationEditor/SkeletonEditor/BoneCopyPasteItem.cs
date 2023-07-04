using CommonControls.Common;
using View3D.Animation;

namespace AnimationEditor.SkeletonEditor
{
    public class BoneCopyPasteItem : ICopyPastItem
    {
        public string Description { get; set; } = "Copy object for bones";
        public GameSkeleton SourceSkeleton { get; set; }
        public int BoneIndex { get; set; }
    }
}
