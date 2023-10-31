using CommonControls.Common;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
//using AssetManagement.Strategies.Fbx.ExportDIalog;

namespace AssetEditor.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Point _lastMouseDown;
        IEditorViewModel draggedItem;

        public MainWindow()
        {
            InitializeComponent();                       
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Title = $"{fvi.ProductName} - {fvi.FileVersion}";
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

                    draggedItem = item.DataContext as IEditorViewModel;
                }
            }
            catch
            {
                // TODO: huh?
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
                        if (draggedItem != null)
                        {
                            DragDrop.DoDragDrop(EditorsTabControl, EditorsTabControl.SelectedValue, DragDropEffects.Move);
                        }
                    }
                }
                else
                {
                    draggedItem = null;
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
                    if (draggedItem == null)
                        return;

                    var dropTargetNode = dropTargetItem?.DataContext as IEditorViewModel;
                    if (dropTargetNode == null)
                        return;

                    if (dropContainer.AllowDrop(draggedItem, dropTargetNode, insertAfterTargetNode))
                    {
                        dropContainer.Drop(draggedItem, dropTargetNode, insertAfterTargetNode);
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
