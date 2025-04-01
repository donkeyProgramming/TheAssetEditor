using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.Events;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor
{
    public partial class SceneNodeEditorViewModel : ObservableObject, IDisposable
    {
        private readonly IEventHub _eventHub;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _map = [];

        [ObservableProperty] ISceneNodeEditor? _currentEditor;

        public SceneNodeEditorViewModel(IEventHub eventHub, IServiceProvider serviceProvider)
        {
            _eventHub = eventHub;
            _serviceProvider = serviceProvider;

            _map[typeof(MainEditableNode)] = typeof(MainEditableNodeViewModel);
            _map[typeof(Rmv2MeshNode)] = typeof(MeshEditorViewModel);
            _map[typeof(SkeletonNode)] = typeof(SkeletonSceneNodeViewModel);
            _map[typeof(GroupNode)] = typeof(GroupNodeViewModel);

            _eventHub.Register<SceneNodeSelectedEvent>(this, OnSelectionChanged);
        }

        void OnSelectionChanged(SceneNodeSelectedEvent selectionChangedEvent)
        {
            if (selectionChangedEvent.SelectedObjects.Count != 1)
            {
                CurrentEditor = null;
                return;
            }

            var selectedNode = selectionChangedEvent.SelectedObjects.First();

            CurrentEditor?.Dispose();
            CurrentEditor = null;
            CurrentEditor = CreateNodeEditor(selectedNode);
        }

        ISceneNodeEditor? CreateNodeEditor(ISceneNode node)
        {
            if (node is GroupNode groupNode)
            {
                if(groupNode.Name == SpecialNodes.ReferenceMeshs)
                    return null;

                if (groupNode.Name == SpecialNodes.Root)
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
            _eventHub.UnRegister(this);
            CurrentEditor?.Dispose();
        }
    }
}
