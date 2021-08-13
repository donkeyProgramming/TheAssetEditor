using CommonControls.Behaviors;
using KitbasherEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    public class MultiSelectTreeView : TreeView
    {
        public ObservableCollection<ISceneNode> SelectedObjects
        {
            get { return (ObservableCollection<ISceneNode>)GetValue(SelectedObjectsProperty); }
            set { SetValue(SelectedObjectsProperty, value); }
        }

        public static readonly DependencyProperty SelectedObjectsProperty =
            DependencyProperty.Register("SelectedObjects", typeof(ObservableCollection<ISceneNode>), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(OnSelectionCollectionAssigned));

        private static void OnSelectionCollectionAssigned(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as MultiSelectTreeView;
            if (e.NewValue == null)
                tb.UnSubscribe(e.OldValue as ObservableCollection<ISceneNode>);
            else
                tb.Subscribe(e.NewValue as ObservableCollection<ISceneNode>);
        }

        public MultiSelectTreeView()
        {
            SelectedItemChanged += MyTreeView_SelectedItemChanged;
            Focusable = true;
            PreviewMouseDoubleClick += MultiSelectTreeView_PreviewMouseDoubleClick;   
        }

        private void MultiSelectTreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                var node = treeViewItem.DataContext as SceneNode;
                if (node != null)
                    node.IsExpanded = !node.IsExpanded;
            }

            e.Handled = true;
        }

        public static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        void Deselect(TreeViewItem treeViewItem, ISceneNode node)
        {
            if (treeViewItem != null)
            {
                treeViewItem.Background = Brushes.White;
                treeViewItem.Foreground = Brushes.Black;
            }
            SelectedObjects.Remove(node);
        }

        void ChangeSelectedState(TreeViewItem treeViewItem, ISceneNode node)
        {
            if (!SelectedObjects.Contains(node))
            {
                treeViewItem.Background = Brushes.LightBlue;
                treeViewItem.Foreground = Brushes.Black;
                SelectedObjects.Add(node);
            }
            else
            {
                Deselect(treeViewItem, node);
            }
        }

        void MyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObjects == null)
                return;

            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;
                var snode = SelectedItem as ISceneNode;
                var treeViewItem = ItemContainerGenerator.ContainerFromItemRecursive(SelectedItem);
                if (treeViewItem == null)
                    return;

                treeViewItem.IsSelected = false;
                treeViewItem.Focus();

                if (!IsShiftPressed)
                {
                    var itemsToDelete = SelectedObjects.Select(x => x).ToList();
                    foreach (var item in itemsToDelete)
                    {
                        var itts = ItemContainerGenerator.ContainerFromItemRecursive(item);
     
                            Deselect(itts, item);
                    }
                }

                ChangeSelectedState(treeViewItem, snode);
            }
            finally
            {
                SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
            }
        }

        public void Subscribe(ObservableCollection<ISceneNode> collection)
        {
            collection.CollectionChanged += SelectedObjects_CollectionChanged;
        }

        public void UnSubscribe(ObservableCollection<ISceneNode> collection)
        {
            collection.CollectionChanged -= SelectedObjects_CollectionChanged;
        }

        private void SelectedObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (SelectedObjects == null)
                return;

            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;
                SelectedItemChanged -= MyTreeView_SelectedItemChanged;

                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (var item in e.NewItems)
                    {
                        var treeViewItem = ItemContainerGenerator.ContainerFromItemRecursive(item);
                        if (treeViewItem != null)
                        {
                            treeViewItem.Background = Brushes.LightBlue;
                            treeViewItem.Foreground = Brushes.Black;
                            treeViewItem.IsSelected = false;
                            treeViewItem.Focus();
                        }
                    }
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    foreach (var item in e.OldItems)
                    {
                        var treeViewItem = ItemContainerGenerator.ContainerFromItemRecursive(item);
                        if (treeViewItem != null)
                        {
                            treeViewItem.Background = Brushes.White;
                            treeViewItem.Foreground = Brushes.Black;
                            treeViewItem.IsSelected = false;
                        }
                    }
                }
                else
                {
                    throw new Exception("Unknown event in MultiSelectTreeView::SelectedObjects_CollectionChanged " + e.Action);
                }
            }
            finally
            {
                SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
                SelectedItemChanged += MyTreeView_SelectedItemChanged;
            }
        }

        bool IsShiftPressed { get =>  Keyboard.IsKeyDown(Key.LeftShift);}
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
