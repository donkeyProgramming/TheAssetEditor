using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public interface ISceneNodeEditor : IDisposable
    {
        void Initialize(ISceneNode node);
    }
}
