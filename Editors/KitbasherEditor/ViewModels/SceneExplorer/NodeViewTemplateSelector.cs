using System.Windows;
using System.Windows.Controls;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes;
using KitbasherEditor.Views.EditorViews;
using KitbasherEditor.Views.EditorViews.Rmv2;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer
{
    public class NodeViewTemplateSelector : DataTemplateSelector
    {
        private readonly Dictionary<Type, Type> _store = new();

        public NodeViewTemplateSelector()
        {
            _store[typeof(MainEditableNodeViewModel)] = typeof(MainEditableNodeView);
            _store[typeof(SkeletonSceneNodeViewModel)] = typeof(SkeletonView);
            _store[typeof(GroupNodeViewModel)] = typeof(GroupView);
            _store[typeof(MeshEditorViewModel)] = typeof(MeshEditorView);
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return base.SelectTemplate(item, container);

            var viewModelType = item.GetType();
            var found = _store.TryGetValue(viewModelType, out var viewType);
            if (found == false)
                throw new Exception($"Unable to determine ViewModel for input type {viewModelType}");

            var factory = new FrameworkElementFactory(viewType);
            var dt = new DataTemplate
            {
                VisualTree = factory
            };

            return dt;
        }
    }
}
