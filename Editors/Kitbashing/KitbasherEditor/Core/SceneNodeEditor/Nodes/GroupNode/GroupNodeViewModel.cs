using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views.EditorViews;
using Shared.Ui.Common.DataTemplates;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class GroupNodeViewModel : ObservableObject, ISceneNodeEditor, IViewProvider<GroupView>
    {
        ISceneNode _node;

        [ObservableProperty] string _groupName = string.Empty;

        public void Initialize(ISceneNode node)
        {
            _node = node;
            GroupName = _node.Name;
        }

        partial void OnGroupNameChanged(string value) => _node.Name = value;

        public void Dispose(){}
    }
}
