using GameWorld.Core.Animation;

namespace Editors.KitbasherEditor.Events
{
    public class KitbasherSkeletonChangedEvent
    {
        public GameSkeleton Skeleton { get; init; }
        public string SkeletonName { get; init; }
    }
}
