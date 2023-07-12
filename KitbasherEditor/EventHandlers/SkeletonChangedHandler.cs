using Common;
using KitbasherEditor.Events;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.EventHandlers
{
    public class SkeletonChangedHandler
    {
        SceneManager _sceneManager;

        public SkeletonChangedHandler( EventHub eventHub, SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
            eventHub.Register<KitbasherSkeletonChangedEvent>(OnSkeletonChanged);
        }

        private void OnSkeletonChanged(KitbasherSkeletonChangedEvent e)
        {
            _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel).SkeletonNode.Skeleton = e.Skeleton;
        }
    }
}
