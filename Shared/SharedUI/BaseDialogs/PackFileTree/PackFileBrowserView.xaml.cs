using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    public partial class PackFileBrowserView : UserControl
    {
        public PackFileBrowserView()
        {
            InitializeComponent();
        }

        Point _lastMouseDown;
        TreeNode? _draggedItem;

        public System.Windows.Controls.ContextMenu CustomContextMenu
        {
            get { return (System.Windows.Controls.ContextMenu)GetValue(CustomContextMenuProperty); }
            set { SetValue(CustomContextMenuProperty, value); }
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = (TreeViewItem)sender;

                _lastMouseDown = e.GetPosition(tvParameters);

                _draggedItem = item.DataContext as TreeNode;
            }
        }

        public void TriggerPreviewKeyDown()
        {
            var args = new KeyEventArgs(InputManager.Current.PrimaryKeyboardDevice, PresentationSource.FromVisual(this), 0, Key.F)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && args.Key == Key.F)
            {
                FilterTextBoxItem.Focus();
                FilterTextBoxItem.SelectAll();
                args.Handled = true;
            }
        }

        private void treeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(tvParameters);

                if (Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0 ||
                    Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0)
                {
                    if (_draggedItem != null)
                    {
                        DragDrop.DoDragDrop(tvParameters, tvParameters.SelectedValue, DragDropEffects.Move);
                    }
                }
            }
            else
            {
                _draggedItem = null;
            }
        }

        private void treeView_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is IDropTarget<TreeNode> dropContainer)
            {
                if (_draggedItem == null)
                    return;

                var dropTargetItem = sender as TreeViewItem;
                var dropTargetNode = dropTargetItem?.DataContext as TreeNode;
                if (dropTargetNode == null)
                    return;

                if (dropContainer.AllowDrop(_draggedItem, dropTargetNode))
                {
                    dropContainer.Drop(_draggedItem, dropTargetNode);
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            FilterTextBoxItem.Focus();
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item != null)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        public static readonly DependencyProperty CustomContextMenuProperty = DependencyProperty.Register("CustomContextMenu", typeof(System.Windows.Controls.ContextMenu), typeof(PackFileBrowserView), new UIPropertyMetadata(null));
    }
}
