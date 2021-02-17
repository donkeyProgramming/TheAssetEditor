using KitbasherEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.Views
{
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
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is SceneNode)
            {
                SceneNode sceneElement = item as SceneNode;
                if(sceneElement.IsEditable)
                    return element.FindResource("DefaultTreeNodeStyle") as HierarchicalDataTemplate;
                else
                    return element.FindResource("ReferenceTreeNodeStyle") as HierarchicalDataTemplate;

            }

            return null;
        }
    }
}
