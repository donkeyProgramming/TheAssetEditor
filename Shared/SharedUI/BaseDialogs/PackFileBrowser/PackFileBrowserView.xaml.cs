using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;

namespace CommonControls.PackFileBrowser
{
    public partial class PackFileBrowserView : UserControl
    {
        public PackFileBrowserView()
        {
            InitializeComponent();
        }

        Point _lastMouseDown;
        TreeNode? _draggedItem;

        public ContextMenu CustomContextMenu
        {
            get { return (ContextMenu)GetValue(CustomContextMenuProperty); }
            set { SetValue(CustomContextMenuProperty, value); }
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                TreeViewItem item = (TreeViewItem)sender;

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

                if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                    (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
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

        public static readonly DependencyProperty CustomContextMenuProperty = DependencyProperty.Register("CustomContextMenu", typeof(ContextMenu), typeof(PackFileBrowserView), new UIPropertyMetadata(null));
    }


    public class SortedCollectionViewSource : IValueConverter
    {
        public string Property0 { get; set; }
        public string Property1 { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = CollectionViewSource.GetDefaultView(value);
            s.SortDescriptions.Add(new SortDescription(Property0, ListSortDirection.Ascending));
            s.SortDescriptions.Add(new SortDescription(Property1, ListSortDirection.Ascending));
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
