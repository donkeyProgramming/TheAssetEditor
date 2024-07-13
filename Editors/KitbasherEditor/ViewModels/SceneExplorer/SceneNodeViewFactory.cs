using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using GameWorld.Core.SceneNodes;
using Microsoft.Extensions.DependencyInjection;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer
{
    public class SceneNodeViewFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _map = [];

        public SceneNodeViewFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _map[typeof(MainEditableNode)] = typeof(MainEditableNodeViewModel);
            _map[typeof(Rmv2MeshNode)] = typeof(MeshEditorViewModel);
            _map[typeof(SkeletonNode)] = typeof(SkeletonSceneNodeViewModel);
            _map[typeof(GroupNode)] = typeof(GroupNodeViewModel);
        }

        public ISceneNodeViewModel? CreateEditorView(ISceneNode node)
        {
            // Special case where nothing should be displayed - The root node
            if(node is GroupNode groupNode) 
            {
                if (groupNode.IsEditable == true && groupNode.Parent == null)
                    return null;
            }

            var found = _map.TryGetValue(node.GetType(), out var viewModelType);
            if (found == false)
                return null;

            var viewModel = _serviceProvider.GetRequiredService(viewModelType!) as ISceneNodeViewModel;
            if (viewModel == null)
                throw new Exception($"{viewModelType} is not of type ISceneNodeViewModel");
            viewModel.Initialize(node);
            return viewModel;
        }
    }
}
