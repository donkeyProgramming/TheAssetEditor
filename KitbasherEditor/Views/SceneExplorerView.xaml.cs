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
            if (treeView.SelectedItem is Node item)
                item.IsChecked = !item.IsChecked;
            e.Handled = true;
        }
    }

    public class TreeItemDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is Node)
            {
                Node sceneElement = item as Node;
                if(sceneElement.IsReference)
                        return element.FindResource("SlotTreeItemTemplate") as HierarchicalDataTemplate; 
                else
                        return element.FindResource("DefaultTreeItemTemplate") as HierarchicalDataTemplate;

            }

            return null;
        }
    }
}
