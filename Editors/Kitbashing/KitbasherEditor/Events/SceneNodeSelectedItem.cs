using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.Events
{
    public record SceneNodeSelectedEvent(List<ISceneNode> SelectedObjects);

}
