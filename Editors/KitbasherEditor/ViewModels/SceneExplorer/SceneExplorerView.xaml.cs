using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameWorld.Core.SceneNodes;

namespace KitbasherEditor.Views
{

    public static class ItemContainerGeneratorHelper
    {
        public static TreeViewItem ContainerFromItemRecursive(this ItemContainerGenerator root, object item)
        {
            var treeViewItem = root.ContainerFromItem(item) as TreeViewItem;
            if (treeViewItem != null)
                return treeViewItem;
            foreach (var subItem in root.Items)
            {
                treeViewItem = root.ContainerFromItem(subItem) as TreeViewItem;
                var search = treeViewItem?.ItemContainerGenerator.ContainerFromItemRecursive(item);
                if (search != null)
                    return search;
            }
            return null;
        }
    }

    /// <summary>
    /// Interaction logic for SceneExplorerView.xaml
    /// </summary>
    public partial class SceneExplorerView : UserControl
    {
        public SceneExplorerView()
        {
            InitializeComponent();
        }

        private void TreeViewItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView.SelectedItem is SceneNode item)
                item.IsExpanded = !item.IsExpanded;
            e.Handled = true;
        }


    }

    public class TreeItemDataTemplateSelector : DataTemplateSelector
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
