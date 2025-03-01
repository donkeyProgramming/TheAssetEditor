using System.IO;
using Editors.KitbasherEditor.Events;
using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.KitbasherEditor.Core
{
    public class KitbasherRootScene : ISkeletonProvider
    {
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;

        public GameSkeleton Skeleton { get; private set; }
        public AnimationPlayer Player { get; private set; }
     
        public KitbasherRootScene(AnimationsContainerComponent animationsContainerComponent, IPackFileService packFileService, IEventHub eventHub)
        {
            _animationsContainerComponent = animationsContainerComponent;
            _packFileService = packFileService;
            _eventHub = eventHub;
            Player = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), "MainPlayer");
        }

        public void SetSkeletonFromName(string skeletonName)
        {
            var cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(skeletonName))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);

            var animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + cleanSkeletonName + ".anim";
            var skeletonPfs = _packFileService.FindFile(skeletonFilePath);
            if (skeletonPfs != null)
            {
                var animClip = AnimationFile.Create(skeletonPfs);
                Skeleton = new GameSkeleton(animClip, Player);
            }

            _eventHub.Publish(new KitbasherSkeletonChangedEvent() { Skeleton = Skeleton, SkeletonName = skeletonName });
        }
    }
}
