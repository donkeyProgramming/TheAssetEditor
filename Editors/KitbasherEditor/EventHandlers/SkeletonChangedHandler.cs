using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Events;
using Shared.Core.Events;

namespace KitbasherEditor.EventHandlers
{
    public class SkeletonChangedHandler
    {
        private readonly SceneManager _sceneManager;

        public SkeletonChangedHandler(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Subscribe(EventHub eventHub)
        {
            eventHub.Register<KitbasherSkeletonChangedEvent>(OnSkeletonChanged);
        }

        private void OnSkeletonChanged(KitbasherSkeletonChangedEvent e)
        {
            _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel).SkeletonNode.Skeleton = e.Skeleton;
        }
    }
}
