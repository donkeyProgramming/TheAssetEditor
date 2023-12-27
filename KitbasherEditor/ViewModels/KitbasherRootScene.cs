using System.IO;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using KitbasherEditor.Events;
using Monogame.WpfInterop.Common;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels
{
    public class KitbasherRootScene : ISkeletonProvider, IActiveFileResolver
    {
        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly PackFileService _packFileService;
        private readonly EventHub _eventHub;

        public KitbasherRootScene(AnimationsContainerComponent animationsContainerComponent, PackFileService packFileService, EventHub eventHub)
        {
            _animationsContainerComponent = animationsContainerComponent;
            _packFileService = packFileService;
            _eventHub = eventHub;
            Player = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), "MainPlayer");
        }

        public GameSkeleton Skeleton { get; private set; }
        public RmvVersionEnum SelectedOutputFormat { get; set; }    // TODO:Remoev this
        public AnimationPlayer Player { get; private set; }

        public void SetSkeletonFromName(string skeletonName)
        {
            string cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(skeletonName))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);

            string animationFolder = "animations\\skeletons\\";
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
