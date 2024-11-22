using Editors.KitbasherEditor.Events;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Shared.Core.Events;

namespace Editors.KitbasherEditor.EventHandlers
{
    public class SkeletonChangedHandler
    {
        private readonly SceneManager _sceneManager;

        public SkeletonChangedHandler(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Subscribe(IEventHub eventHub)
        {
            eventHub.Register<KitbasherSkeletonChangedEvent>(this, OnSkeletonChanged);
        }

        private void OnSkeletonChanged(KitbasherSkeletonChangedEvent e)
        {
            _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel).SkeletonNode.Skeleton = e.Skeleton;
        }
    }
}
