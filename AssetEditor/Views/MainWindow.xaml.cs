using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.Common;

namespace AssetEditor.Views
{
    public partial class MainWindow : Window
    {
        Point _lastMouseDown;
        IEditorViewModel _draggedItem;

        public MainWindow()
        {
            InitializeComponent();
            Title = $"AssetEditor v{VersionChecker.CurrentVersion}";
        }

        private void tabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    _lastMouseDown = e.GetPosition(EditorsTabControl);

                    var item = (TabItem)sender;

                    item.Focusable = true;
                    item.Focus();
                    item.Focusable = false;

                    _draggedItem = item.DataContext as IEditorViewModel;
                }
            }
            catch
            {
            }
        }

        private void tabItem_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPosition = e.GetPosition(EditorsTabControl);

                    if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                    {
                        if (_draggedItem != null)
                        {
                            DragDrop.DoDragDrop(EditorsTabControl, EditorsTabControl.SelectedValue, DragDropEffects.Move);
                        }
                    }
                }
                else
                {
                    _draggedItem = null;
                }
            }
            catch
            {
            }
        }

        private void tabItem_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var dropTargetItem = sender as TabItem;
                var pos = e.GetPosition(dropTargetItem);
                bool insertAfterTargetNode = pos.X - dropTargetItem.ActualWidth / 2 > 0;

                if (DataContext is IDropTarget<IEditorViewModel, bool> dropContainer)
                {
                    if (_draggedItem == null)
                        return;

                    var dropTargetNode = dropTargetItem?.DataContext as IEditorViewModel;
                    if (dropTargetNode == null)
                        return;

                    if (dropContainer.AllowDrop(_draggedItem, dropTargetNode, insertAfterTargetNode))
                    {
                        dropContainer.Drop(_draggedItem, dropTargetNode, insertAfterTargetNode);
                        e.Effects = DragDropEffects.None;
                        e.Handled = true;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
