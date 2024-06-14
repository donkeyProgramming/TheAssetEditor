using GameWorld.Core.Animation;

namespace KitbasherEditor.Events
{
    public class KitbasherSkeletonChangedEvent
    {
        public GameSkeleton Skeleton { get; init; }
        public string SkeletonName { get; init; }
    }
}
