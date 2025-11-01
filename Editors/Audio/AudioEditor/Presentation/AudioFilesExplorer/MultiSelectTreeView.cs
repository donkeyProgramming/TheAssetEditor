using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Editors.Audio.AudioEditor.Presentation.Shared;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer
{
    public class MultiSelectTreeView : TreeView
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectTreeView), new PropertyMetadata(new ArrayList(), OnSelectedItemsChanged));
        public static readonly DependencyProperty IsMultiSelectedProperty = DependencyProperty.RegisterAttached("IsMultiSelected", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(false));

        private AudioFilesTreeNode _anchorItem;
        private AudioFilesTreeNode _pendingClickItem;
        private Point _dragStartPoint;
        private bool _isDragOperation;
        private bool _suppressSelectionChange;
        private List<AudioFilesTreeNode> _preDragSelectedNodes;

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
            PreviewMouseMove += MultiSelectTreeView_PreviewMouseMove;
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectTreeView treeView)
                treeView.UpdateSelectionStates();
        }

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressSelectionChange)
                return;

            UpdateSelectionStates();
        }

        public static bool GetIsMultiSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMultiSelectedProperty);
        }

        private static void SetIsMultiSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMultiSelectedProperty, value);
        }

        private void MultiSelectTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            _preDragSelectedNodes = SelectedItems.OfType<AudioFilesTreeNode>().ToList();
            _isDragOperation = false;
            _pendingClickItem = null;

            var itemUnderMouse = GetTreeViewItemUnderMouse(e);
            if (itemUnderMouse != null)
            {
                var clickTarget = e.OriginalSource as DependencyObject;
                var clickedExpander = IsClickOnExpander(clickTarget);

                if (!clickedExpander &&
                    itemUnderMouse.DataContext is AudioFilesTreeNode clickedNode &&
                    SelectedItems.Contains(clickedNode) &&
                    !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                    !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    _pendingClickItem = clickedNode;
                    e.Handled = true; 
                }
            }
        }

        private void MultiSelectTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var currentPos = e.GetPosition(this);
            var diff = _dragStartPoint - currentPos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (_preDragSelectedNodes?.Count > 0)
                {
                    _isDragOperation = true;
                    _pendingClickItem = null;
                    _suppressSelectionChange = true;

                    try
                    {
                        var data = new DataObject(typeof(IEnumerable<AudioFilesTreeNode>), _preDragSelectedNodes);
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
                    }
                    finally
                    {
                        _suppressSelectionChange = false;
                        Dispatcher.BeginInvoke(UpdateSelectionStates, System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (_pendingClickItem != null && !_isDragOperation)
            {
                // Treat it as a normal single click
                _suppressSelectionChange = true;
                try
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(_pendingClickItem);
                    _anchorItem = _pendingClickItem;
                }
                finally
                {
                    _suppressSelectionChange = false;
                    UpdateSelectionStates();
                }
            }

            // Reset state for the next cycle
            _pendingClickItem = null;
            _isDragOperation = false;
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);

            if (_suppressSelectionChange || e.NewValue is not AudioFilesTreeNode current)
                return;

            _suppressSelectionChange = true;
            try
            {
                SelectedItems ??= new ArrayList();

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (_anchorItem == null)
                    {
                        _anchorItem = current;
                        SelectedItems.Clear();
                        SelectedItems.Add(_anchorItem);
                    }
                    else
                    {
                        SelectRangeFromAnchor(_anchorItem, current);
                    }
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (SelectedItems.Contains(current))
                        SelectedItems.Remove(current);
                    else
                        SelectedItems.Add(current);

                    _anchorItem ??= current;
                }
                else
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(current);
                    _anchorItem = current;
                }
            }
            finally
            {
                _suppressSelectionChange = false;
            }

            UpdateSelectionStates();
        }

        private void SelectRangeFromAnchor(AudioFilesTreeNode anchor, AudioFilesTreeNode target)
        {
            var items = GetTreeViewItems(this).Where(i => i.DataContext is AudioFilesTreeNode)
                                              .ToList();
            var a = items.FindIndex(i => i.DataContext == anchor);
            var t = items.FindIndex(i => i.DataContext == target);
            if (a < 0 || t < 0) return;

            var start = Math.Min(a, t);
            var end = Math.Max(a, t);

            _suppressSelectionChange = true;
            try
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var node = (AudioFilesTreeNode)items[i].DataContext;
                    var shouldBeSelected = i >= start && i <= end;

                    if (shouldBeSelected && !SelectedItems.Contains(node))
                        SelectedItems.Add(node);
                    else if (!shouldBeSelected && SelectedItems.Contains(node))
                        SelectedItems.Remove(node);
                }
            }
            finally
            {
                _suppressSelectionChange = false;
            }

            UpdateSelectionStates();
        }


        private void UpdateSelectionStates()
        {
            if (_suppressSelectionChange)
                return;

            _suppressSelectionChange = true;
            try
            {
                foreach (var item in GetTreeViewItems(this))
                {
                    if (item.DataContext is not AudioFilesTreeNode node)
                        continue;

                    var shouldBeSelected = SelectedItems.Contains(node);

                    //  If an item is not part of our multi-selection but happens to be WPF’s lead item, turn its flag off so the highlight disappears.
                    //  Don't set IsSelected = true here otherwise we re-enter the single-selection process
                    if (!shouldBeSelected && item.IsSelected)
                        item.IsSelected = false;

                    SetIsMultiSelected(item, shouldBeSelected);

                    if (shouldBeSelected)
                    {
                        item.Background =
                            (Brush)Application.Current.Resources["TreeViewItem.Selected.Background"];
                        item.BorderBrush =
                            (Brush)Application.Current.Resources["TreeViewItem.Selected.Border"];
                        item.Foreground =
                            (Brush)Application.Current.Resources["ABrush.Foreground.Deeper"];
                    }
                    else
                    {
                        item.ClearValue(BackgroundProperty);
                        item.ClearValue(BorderBrushProperty);
                        item.ClearValue(ForegroundProperty);
                    }
                }
            }
            finally
            {
                _suppressSelectionChange = false;
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

        private static bool IsClickOnExpander(DependencyObject source)
        {
            while (source != null)
            {
                if (source is ToggleButton toggle && toggle.Name == "Expander")
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
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
