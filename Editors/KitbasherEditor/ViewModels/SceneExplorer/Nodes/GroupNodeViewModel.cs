using GameWorld.Core.SceneNodes;
using Shared.Core.Misc;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public class GroupNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        GroupNode _node;

        public string GroupName { get => _node.Name; set { _node.Name = value; NotifyPropertyChanged(); } }

        public GroupNodeViewModel()
        {
            
        }
        public void Initialize(ISceneNode node)
        {
            _node = node as GroupNode;
        }

        public void Dispose()
        {
        }
    }
}
