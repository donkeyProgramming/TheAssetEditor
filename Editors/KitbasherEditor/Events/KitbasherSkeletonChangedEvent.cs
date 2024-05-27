using View3D.Animation;

namespace KitbasherEditor.Events
{
    public class KitbasherSkeletonChangedEvent
    {
        public GameSkeleton Skeleton { get; init; }
        public string SkeletonName { get; init; }
    }
}
