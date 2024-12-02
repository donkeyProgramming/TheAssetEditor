using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameWorld.Core.SceneNodes;

namespace KitbasherEditor.Views
{
    public class MultiSelectTreeView : TreeView
    {
        private SolidColorBrush _selectedBackgroundBrush { get; } = new SolidColorBrush(Color.FromRgb(255, 0, 0));//= new SolidColorBrush(Color.FromRgb(58, 58, 58));
        private SolidColorBrush _selectedForegroundBrush { get; } = new SolidColorBrush(Color.FromRgb(255, 0, 0));

        private SolidColorBrush _notSelectedBackgroundBrush { get; } = new SolidColorBrush(Color.FromRgb(36, 36, 36));
        private SolidColorBrush _notSelectedForegroundBrush { get; } = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        TreeViewItem? _lastItemSelected;

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
            _selectedBackgroundBrush = (SolidColorBrush)FindResource("TreeViewItem.Selected.Background");
            _notSelectedBackgroundBrush = (SolidColorBrush)FindResource("TreeView.Static.Background");

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
                treeViewItem.Background = _notSelectedBackgroundBrush;
                treeViewItem.Foreground = _notSelectedForegroundBrush;
            }
            SelectedObjects.Remove(node);
        }


        void Select(TreeViewItem treeViewItem, ISceneNode node)
        {
            if (treeViewItem != null)
            {
                treeViewItem.Background = _selectedBackgroundBrush;
                treeViewItem.Foreground = _selectedBackgroundBrush;
            }

            if (node != null)
            {
                if (SelectedObjects.Contains(node) == false)
                    SelectedObjects.Add(node);
            }
        }




        void MyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObjects == null)
                return;

            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;
                SelectedItemChanged -= MyTreeView_SelectedItemChanged;

                var treeViewItem = ItemContainerGenerator.ContainerFromItemRecursive(SelectedItem);
                if (treeViewItem == null)
                    return;

                treeViewItem.IsSelected = false;
                treeViewItem.Focus();

                if (!IsCtrlPressed)
                {
                    var items = GetTreeViewItems(this, true);
                    foreach (var i in items)
                        Deselect(i, i.DataContext as ISceneNode);
                }
    
                if (IsShiftPressed && _lastItemSelected != null)
                {
                    var items = GetTreeViewItemRange(_lastItemSelected, treeViewItem);
                    foreach (var i in items)
                    {
                        Select(i, i.DataContext as ISceneNode);
                    }
                   
                }
                else
                {
                    _lastItemSelected = treeViewItem;
                    Select(treeViewItem, treeViewItem.DataContext as ISceneNode);
                }


            }
            finally
            {
                SelectedItemChanged += MyTreeView_SelectedItemChanged;
                SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
            }
        }

        private static List<TreeViewItem> GetTreeViewItems(ItemsControl parentItem, bool includeCollapsedItems, List<TreeViewItem> itemList = null)
        {
            if (itemList == null)
                itemList = new List<TreeViewItem>();

            for (var index = 0; index < parentItem.Items.Count; index++)
            {
                var tvItem = parentItem.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
                if (tvItem == null) continue;

                itemList.Add(tvItem);
                if (includeCollapsedItems || tvItem.IsExpanded)
                    GetTreeViewItems(tvItem, includeCollapsedItems, itemList);
            }
            return itemList;
        }
        private List<TreeViewItem> GetTreeViewItemRange(TreeViewItem start, TreeViewItem end)
        {
            var items = GetTreeViewItems(this, false);

            var startIndex = items.IndexOf(start);
            var endIndex = items.IndexOf(end);
            var rangeStart = startIndex > endIndex || startIndex == -1 ? endIndex : startIndex;
            var rangeCount = startIndex > endIndex ? startIndex - endIndex + 1 : endIndex - startIndex + 1;

            if (startIndex == -1 && endIndex == -1)
                rangeCount = 0;

            else if (startIndex == -1 || endIndex == -1)
                rangeCount = 1;

            return rangeCount > 0 ? items.GetRange(rangeStart, rangeCount) : new List<TreeViewItem>();
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
                            treeViewItem.Background = _selectedBackgroundBrush;
                            treeViewItem.Foreground = _selectedForegroundBrush;
                            treeViewItem.IsSelected = true;
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
                            treeViewItem.Background = _notSelectedBackgroundBrush;
                            treeViewItem.Foreground = _notSelectedForegroundBrush;
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

        private static bool IsCtrlPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl); }
        }
        private static bool IsShiftPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift); }
        }

    }

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
}
