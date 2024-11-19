using System.Windows;
using System.Windows.Controls;
using GameWorld.Core.SceneNodes;

namespace KitbasherEditor.Views
{
    public class SceneNodeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            try
            {
                var element = container as FrameworkElement;
                var sceneElement = item as SceneNode;

                if (element != null && sceneElement != null)
                {
                    if (sceneElement.IsEditable)
                        return element.FindResource("DefaultTreeNodeStyle") as HierarchicalDataTemplate;
                    else
                        return element.FindResource("ReferenceTreeNodeStyle") as HierarchicalDataTemplate;
                }

                return base.SelectTemplate(item, container);
            }
            catch
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
