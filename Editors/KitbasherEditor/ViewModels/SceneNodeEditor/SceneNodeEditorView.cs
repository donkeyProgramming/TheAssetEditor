using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor
{
    public partial class SceneNodeEditorViewModel : ObservableObject, IDisposable
    {
        private readonly EventHub _eventHub;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _map = [];

        [ObservableProperty] ISceneNodeEditor? _currentEditor;

        public SceneNodeEditorViewModel(EventHub eventHub, IServiceProvider serviceProvider)
        {
            _eventHub = eventHub;
            _serviceProvider = serviceProvider;

            _map[typeof(MainEditableNode)] = typeof(MainEditableNodeViewModel);
            _map[typeof(Rmv2MeshNode)] = typeof(MeshEditorViewModel);
            _map[typeof(SkeletonNode)] = typeof(SkeletonSceneNodeViewModel);
            _map[typeof(GroupNode)] = typeof(GroupNodeViewModel);

            _eventHub.Register<SelectionChangedEvent>(OnSelectionChanged);
        }

        void OnSelectionChanged(SelectionChangedEvent selectionChangedEvent)
        {
            if (selectionChangedEvent.NewState.Mode != GeometrySelectionMode.Object)
            {
                CurrentEditor = null;
                return;
            }

            var objectState = selectionChangedEvent.NewState as ObjectSelectionState;
            if (objectState!.SelectionCount() != 1)
            {
                CurrentEditor = null;
                return;
            }

            var selectedNode = objectState.GetSingleSelectedObject();
            CurrentEditor = CreateNodeEditor(selectedNode);
        }

        public ISceneNodeEditor? CreateNodeEditor(ISceneNode node)
        {
            // Special case where nothing should be displayed - The root node
            if (node is GroupNode groupNode)
            {
                if (groupNode.IsEditable == true && groupNode.Parent == null)
                    return null;
            }

            var found = _map.TryGetValue(node.GetType(), out var viewModelType);
            if (found == false)
                return null;

            var viewModel = _serviceProvider.GetRequiredService(viewModelType!) as ISceneNodeEditor;
            if (viewModel == null)
                throw new Exception($"{viewModelType} is not of type ISceneNodeViewModel");
            viewModel.Initialize(node);
            return viewModel;
        }


        public void Dispose()
        {
            _eventHub.UnRegister<SelectionChangedEvent>(OnSelectionChanged);
        }
    }
}
