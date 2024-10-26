using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AssetEditor.WindowsTitleMenu;
using CommonControls.PackFileBrowser;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.Common;

namespace AssetEditor.Views
{
    public partial class MainWindow : Window
    {
        Point _lastMouseDown;
        IEditorInterface _draggedItem;

        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly GameInformationFactory _gameInformationFactory;

        public MainWindow(ApplicationSettingsService applicationSettingsService, GameInformationFactory gameInformationFactory)
        {
            _applicationSettingsService = applicationSettingsService;
            _gameInformationFactory = gameInformationFactory;

            InitializeComponent();
            SourceInitialized += OnSourceInitialized;

            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (e.Key == Key.F)
                    {
                        var packFileBrowserView = FindChild<PackFileBrowserView>(this);

                        if (packFileBrowserView != null) { }
                            packFileBrowserView.TriggerPreviewKeyDown();
                    }
                }
            }
        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
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

                    _draggedItem = item.DataContext as IEditorInterface;
                }
            }
            catch
            {
            }
        }

        private void TabItem_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var currentPosition = e.GetPosition(EditorsTabControl);

                    if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                    {
                        if (_draggedItem != null)
                            DragDrop.DoDragDrop(EditorsTabControl, EditorsTabControl.SelectedValue, DragDropEffects.Move);
                    }
                }
                else
                    _draggedItem = null;
            }
            catch
            {
            }
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var dropTargetItem = sender as TabItem;
                var pos = e.GetPosition(dropTargetItem);
                var insertAfterTargetNode = pos.X - dropTargetItem.ActualWidth / 2 > 0;

                if (DataContext is IDropTarget<IEditorInterface, bool> dropContainer)
                {
                    if (_draggedItem == null)
                        return;

                    if (dropTargetItem?.DataContext is not IEditorInterface dropTargetNode)
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

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeHelpers.WM_NCHITTEST:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        // Return HTMAXBUTTON when the mouse is over the maximize/restore button
                        var point = PointFromScreen(new Point(lParam.ToInt32() & 0xFFFF, lParam.ToInt32() >> 16));
                        if (WpfHelpers.GetElementBoundsRelativeToWindow(maximizeRestoreButton, this).Contains(point))
                        {
                            handled = true;
                            // Apply hover button style
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBar.Button.MouseOver.Background"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBar.Button.MouseOver.Foreground"];
                            return new IntPtr(NativeHelpers.HTMAXBUTTON);
                        }
                        else
                        {
                            // Apply default button style (cursor is not on the button)
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBar.Button.Background"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBar.Button.Foreground"];
                        }
                    }
                    break;
                case NativeHelpers.WM_NCLBUTTONDOWN:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        if (wParam.ToInt32() == NativeHelpers.HTMAXBUTTON)
                        {
                            handled = true;
                            // Apply pressed button style
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBar.Button.Pressed.Background"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBar.Button.Pressed.Foreground"];
                        }
                    }
                    break;
                case NativeHelpers.WM_NCLBUTTONUP:
                    if (NativeHelpers.IsSnapLayoutEnabled())
                    {
                        if (wParam.ToInt32() == NativeHelpers.HTMAXBUTTON)
                        {
                            // Apply default button style
                            maximizeRestoreButton.Background = (Brush)App.Current.Resources["TitleBar.Button.Background"];
                            maximizeRestoreButton.Foreground = (Brush)App.Current.Resources["TitleBar.Button.Foreground"];
                            // Maximize or restore the window
                            ToggleWindowState();
                        }
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void MaximizeRestoreButton_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            maximizeRestoreButton.ToolTip = WindowState == WindowState.Normal ? "Maximize" : "Restore";
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void NewWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var w = new MainWindow(_applicationSettingsService, _gameInformationFactory);
            w.WindowState = WindowState.Normal;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Show();
        }

        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                Close();
            else if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
                ShowSystemMenu(e.GetPosition(this));
        }

        public void ToggleWindowState()
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        public void ShowSystemMenu(Point point)
        {
            // Increment coordinates to allow double-click
            ++point.X;
            ++point.Y;
            if (WindowState == WindowState.Normal)
            {
                point.X += Left;
                point.Y += Top;
            }
            SystemCommands.ShowSystemMenu(this, point);
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
