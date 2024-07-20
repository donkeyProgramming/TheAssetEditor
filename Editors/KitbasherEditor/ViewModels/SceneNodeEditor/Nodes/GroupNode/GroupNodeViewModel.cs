using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial  class GroupNodeViewModel : ObservableObject, ISceneNodeEditor
    {
        ISceneNode _node;

        [ObservableProperty] string _groupName = string.Empty;

        public void Initialize(ISceneNode node)
        {
            GroupName = node.Name;
        }

        partial void OnGroupNameChanged(string value) => _node.Name = value;

        public void Dispose(){}
    }
}
