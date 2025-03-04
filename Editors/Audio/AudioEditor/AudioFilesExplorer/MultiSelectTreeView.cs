using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public class MultiSelectTreeView : TreeView
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectTreeView), new PropertyMetadata(new ArrayList(), OnSelectedItemsChanged));
        public static readonly DependencyProperty IsMultiSelectedProperty = DependencyProperty.RegisterAttached("IsMultiSelected", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(false));

        private TreeNode _anchorItem;

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public MultiSelectTreeView()
        {
            if (SelectedItems is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnSelectedItemsCollectionChanged;

            PreviewMouseDown += MultiSelectTreeView_PreviewMouseDown;
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectTreeView treeView)
                treeView.UpdateSelectionStates();
        }

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateSelectionStates();
        }

        public static bool GetIsMultiSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMultiSelectedProperty);
        }

        public static void SetIsMultiSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMultiSelectedProperty, value);
        }

        private void MultiSelectTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var clickedItem = GetTreeViewItemUnderMouse(e);
                if (clickedItem?.DataContext is TreeNode clickedNode)
                {
                    if (SelectedItems.Contains(clickedNode))
                    {
                        SelectedItems.Remove(clickedNode);

                        if (_anchorItem == clickedNode)
                        {
                            _anchorItem = null;
                            clickedItem.IsSelected = false;
                        }
                    }
                    else
                    {
                        SelectedItems.Add(clickedNode);
                        _anchorItem ??= clickedNode;
                    }

                    UpdateSelectionStates();
                    e.Handled = true;
                }
            }
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);

            if (e.NewValue is TreeNode currentSelectedItem)
            {
                SelectedItems ??= new ArrayList();

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (_anchorItem == null)
                    {
                        _anchorItem = currentSelectedItem;
                        SelectedItems.Clear();
                        SelectedItems.Add(_anchorItem);
                    }
                    else
                        SelectRangeFromAnchor(_anchorItem, currentSelectedItem);
                }
                else if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(currentSelectedItem);
                    _anchorItem = currentSelectedItem;
                }

                UpdateSelectionStates();
            }
        }

        private void SelectRangeFromAnchor(TreeNode anchor, TreeNode target)
        {
            var items = GetTreeViewItems(this).Where(item => item.DataContext is TreeNode).ToList();
            var anchorIndex = items.FindIndex(item => item.DataContext == anchor);
            var targetIndex = items.FindIndex(item => item.DataContext == target);

            if (anchorIndex == -1 || targetIndex == -1)
                return;

            var startIndex = Math.Min(anchorIndex, targetIndex);
            var endIndex = Math.Max(anchorIndex, targetIndex);

            for (var i = 0; i < items.Count; i++)
            {
                var node = (TreeNode)items[i].DataContext;

                if (i < startIndex || i > endIndex)
                    SelectedItems.Remove(node);
                else if (!SelectedItems.Contains(node))
                    SelectedItems.Add(node);
            }
        }

        private void UpdateSelectionStates()
        {
            foreach (var item in GetTreeViewItems(this))
            {
                if (item.DataContext is TreeNode node)
                {
                    var isSelected = SelectedItems.Contains(node);
                    SetIsMultiSelected(item, isSelected);

                    if (isSelected)
                    {
                        item.Background = (Brush)Application.Current.Resources["TreeViewItem.Selected.Background"];
                        item.BorderBrush = (Brush)Application.Current.Resources["TreeViewItem.Selected.Border"];
                        item.Foreground = (Brush)Application.Current.Resources["ABrush.Foreground.Deeper"];
                    }
                    else
                    {
                        item.ClearValue(BackgroundProperty);
                        item.ClearValue(BorderBrushProperty);
                        item.ClearValue(ForegroundProperty);
                    }
                }
            }
        }

        private TreeViewItem GetTreeViewItemUnderMouse(MouseButtonEventArgs e)
        {
            var clickedPoint = e.GetPosition(this);
            var hitTestResult = VisualTreeHelper.HitTest(this, clickedPoint);
            var current = hitTestResult?.VisualHit;
            while (current != null && current is not TreeViewItem)
                current = VisualTreeHelper.GetParent(current);
            return current as TreeViewItem;
        }

        private static IEnumerable<TreeViewItem> GetTreeViewItems(ItemsControl parent)
        {
            for (var i = 0; i < parent.Items.Count; i++)
            {
                var item = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromIndex(i);
                if (item != null)
                {
                    yield return item;

                    foreach (var childItem in GetTreeViewItems(item))
                        yield return childItem;
                }
            }
        }
    }
}
