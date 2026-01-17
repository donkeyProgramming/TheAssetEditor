using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Controls
{
    public interface IMultiSelectTreeNode
    {
        bool IsExpanded { get; set; }
        bool IsVisible { get; set; }
        IEnumerable<IMultiSelectTreeNode> Children { get; }
    }

    public class MultiSelectTreeView : TreeView
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems",
            typeof(IList),
            typeof(MultiSelectTreeView),
            new PropertyMetadata(null, OnSelectedItemsChanged));

        public static readonly DependencyProperty IsMultiSelectedProperty = DependencyProperty.RegisterAttached(
            "IsMultiSelected",
            typeof(bool),
            typeof(MultiSelectTreeView),
            new PropertyMetadata(false));

        // The first node clicked (or the nearest to it after removing the initial node by Ctrl + click)
        private IMultiSelectTreeNode? _anchorNode;
        // The node clicked on mouse down that may become a single-click selection on mouse up
        private IMultiSelectTreeNode? _pendingSingleClickNode;
        // Used to ignore selection events resulting from the control applying selection changes
        private bool _isUpdatingNodeSelection;
        private Point _dragStartPoint;
        private bool _isDragInProgress;
        private List<IMultiSelectTreeNode>? _selectedNodesOnDrag;

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public MultiSelectTreeView()
        {
            Loaded += OnLoaded;

            PreviewMouseDown += MultiSelectTreeViewPreviewMouseDown;
            PreviewMouseMove += MultiSelectTreeViewPreviewMouseMove;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            SelectedItems ??= new List<IMultiSelectTreeNode>();
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MultiSelectTreeView treeView)
                return;

            treeView.SetNodeSelectionSafely();
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);

            if (_isUpdatingNodeSelection || e.NewValue is not IMultiSelectTreeNode node)
                return;

            if (SelectedItems == null)
                return;

            _isUpdatingNodeSelection = true;
            try
            {
                var selectedItems = SelectedItems;

                if (IsShiftDown())
                {
                    if (_anchorNode == null)
                        SelectSingleClickNode(node);
                    else
                        SelectNodesFromAnchor(_anchorNode, node);
                }
                else if (IsCtrlDown())
                {
                    if (selectedItems.Contains(node))
                    {
                        selectedItems.Remove(node);

                        if (ReferenceEquals(_anchorNode, node))
                            _anchorNode = FindNearestSelectedNode(node);
                    }
                    else
                    {
                        selectedItems.Add(node);
                        _anchorNode = node;
                    }
                }
                else
                    SelectSingleClickNode(node);

                UpdateNodeSelection();
            }
            finally
            {
                _isUpdatingNodeSelection = false;
            }
        }

        private void MultiSelectTreeViewPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            _pendingSingleClickNode = null;

            if (SelectedItems == null)
                return;

            var selectedItems = SelectedItems;
            _selectedNodesOnDrag = selectedItems.OfType<IMultiSelectTreeNode>().ToList();

            // Ignore double-click here so pending-click and drag detection logic only runs for single-clicks
            if (e.ClickCount == 2)
                return;

            if (e.OriginalSource is not DependencyObject originalSource)
                return;

            if (!TryGetTreeViewItemAndWasExpanderClicked(originalSource, out var treeViewItem, out var wasExpanderClicked))
                return;

            // If the user clicks an already-selected item without Ctrl / Shift it might be the start of a drag so
            // we defer changing selection until mouse up so we don't collapse multi-selection before the drag begins.
            if (!wasExpanderClicked &&
                treeViewItem != null &&
                treeViewItem.DataContext is IMultiSelectTreeNode clickedNode &&
                selectedItems.Contains(clickedNode) &&
                !IsCtrlDown() &&
                !IsShiftDown())
            {
                _pendingSingleClickNode = clickedNode;
                e.Handled = true;
            }
        }

        private void MultiSelectTreeViewPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragInProgress)
                return;

            var currentPosition = e.GetPosition(this);
            var difference = _dragStartPoint - currentPosition;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (_selectedNodesOnDrag?.Count > 0)
                {
                    _isDragInProgress = true;

                    _pendingSingleClickNode = null;
                    _isUpdatingNodeSelection = true;

                    try
                    {
                        var data = new DataObject(typeof(IEnumerable<IMultiSelectTreeNode>), _selectedNodesOnDrag);
                        _selectedNodesOnDrag = null;
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
                    }
                    finally
                    {
                        _isUpdatingNodeSelection = false;
                        Dispatcher.BeginInvoke(SetNodeSelectionSafely, System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (_pendingSingleClickNode != null)
            {
                // Clear the current selection and select the clicked item to treat it as a normal single click since no drag occurred
                _isUpdatingNodeSelection = true;
                try
                {
                    SelectSingleClickNode(_pendingSingleClickNode);
                }
                finally
                {
                    _isUpdatingNodeSelection = false;
                    SetNodeSelectionSafely();
                }
            }

            _pendingSingleClickNode = null;
            _selectedNodesOnDrag = null;
            _isDragInProgress = false;
        }

        private void SetNodeSelectionSafely()
        {
            if (_isUpdatingNodeSelection)
                return;

            if (SelectedItems == null)
                return;

            _isUpdatingNodeSelection = true;
            try
            {
                UpdateNodeSelection();
            }
            finally
            {
                _isUpdatingNodeSelection = false;
            }
        }

        private void UpdateNodeSelection()
        {
            var selectedNodes = SelectedItems?.OfType<IMultiSelectTreeNode>().ToHashSet() ?? [];

            foreach (var item in GetTreeViewItems(this))
            {
                if (item.DataContext is not IMultiSelectTreeNode node)
                    continue;

                var shouldBeSelected = selectedNodes.Contains(node);

                // By default WPF's TreeView always has a selected item for focus and keyboard navigation.
                // If that item is not selected by us we set TreeViewItem.IsSelected to false to remove it
                // without triggering WPF's internal selection highlight logic again.
                if (!shouldBeSelected && item.IsSelected)
                    item.IsSelected = false;

                SetIsMultiSelected(item, shouldBeSelected);
            }
        }

        private void SelectSingleClickNode(IMultiSelectTreeNode node)
        {
            if (SelectedItems == null)
                return;

            var selectedItems = SelectedItems;
            selectedItems.Clear();
            selectedItems.Add(node);
            _anchorNode = node;
        }

        private void SelectNodesFromAnchor(IMultiSelectTreeNode anchorNode, IMultiSelectTreeNode targetNode)
        {
            if (SelectedItems == null)
                return;

            var visibleNodesInDisplayOrder = GetVisibleNodes();

            var anchorIndex = visibleNodesInDisplayOrder.FindIndex(node => ReferenceEquals(node, anchorNode));
            var targetIndex = visibleNodesInDisplayOrder.FindIndex(node => ReferenceEquals(node, targetNode));
            if (anchorIndex < 0 || targetIndex < 0)
                return;

            var startIndex = Math.Min(anchorIndex, targetIndex);
            var endIndex = Math.Max(anchorIndex, targetIndex);

            var selectedNodes = SelectedItems;
            selectedNodes.Clear();

            for (var i = startIndex; i <= endIndex; i++)
                selectedNodes.Add(visibleNodesInDisplayOrder[i]);
        }

        private IMultiSelectTreeNode? FindNearestSelectedNode(IMultiSelectTreeNode removedAnchorNode)
        {
            if (SelectedItems == null)
                return null;

            var selectedItems = SelectedItems;
            if (selectedItems.Count == 0)
                return null;

            var visibleNodesInDisplayOrder = GetVisibleNodes();

            var removedIndex = visibleNodesInDisplayOrder.FindIndex(node => ReferenceEquals(node, removedAnchorNode));
            if (removedIndex < 0)
                return selectedItems.OfType<IMultiSelectTreeNode>().FirstOrDefault();

            for (var i = removedIndex + 1; i < visibleNodesInDisplayOrder.Count; i++)
            {
                if (selectedItems.Contains(visibleNodesInDisplayOrder[i]))
                    return visibleNodesInDisplayOrder[i];
            }

            for (var i = removedIndex - 1; i >= 0; i--)
            {
                if (selectedItems.Contains(visibleNodesInDisplayOrder[i]))
                    return visibleNodesInDisplayOrder[i];
            }

            return null;
        }

        private List<IMultiSelectTreeNode> GetVisibleNodes()
        {
            var visibleNodes = new List<IMultiSelectTreeNode>();
            var rootItems = ItemsSource ?? Items;

            foreach (var item in rootItems)
            {
                if (item is IMultiSelectTreeNode rootNode)
                    GetVisibleNode(rootNode, visibleNodes);
            }

            return visibleNodes;
        }

        private static void GetVisibleNode(IMultiSelectTreeNode node, List<IMultiSelectTreeNode> visibleNodes)
        {
            if (node == null)
                return;

            if (!node.IsVisible)
                return;

            visibleNodes.Add(node);

            if (!node.IsExpanded)
                return;

            foreach (var childNode in node.Children)
                GetVisibleNode(childNode, visibleNodes);
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

        private static bool TryGetTreeViewItemAndWasExpanderClicked(DependencyObject originalSource, out TreeViewItem? treeViewItem, out bool wasExpanderClicked)
        {
            treeViewItem = null;
            wasExpanderClicked = false;

            var currentVisual = originalSource;
            while (currentVisual != null)
            {
                if (currentVisual is ToggleButton toggleButton && toggleButton.Name == "Expander")
                    wasExpanderClicked = true;

                if (currentVisual is TreeViewItem typedTreeViewItem)
                {
                    treeViewItem = typedTreeViewItem;
                    return true;
                }

                currentVisual = VisualTreeHelper.GetParent(currentVisual);
            }

            return false;
        }

        public static bool GetIsMultiSelected(DependencyObject obj) => (bool)obj.GetValue(IsMultiSelectedProperty);

        public static void SetIsMultiSelected(DependencyObject obj, bool value) => obj.SetValue(IsMultiSelectedProperty, value);

        private static bool IsShiftDown() => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        private static bool IsCtrlDown() => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
    }
}
