using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AssetEditor.ViewModels;

namespace AssetEditor.Views.Updater
{
    public partial class UpdaterWindow : Window
    {
        public UpdaterWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnMarkdownScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindParentScrollViewer(sender as DependencyObject);
            if (scrollViewer == null)
                return;

            e.Handled = true;

            var eventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };

            scrollViewer.RaiseEvent(eventArgs);
        }

        private static ScrollViewer FindParentScrollViewer(DependencyObject element)
        {
            while (element != null)
            {
                if (element is ScrollViewer scrollViewer)
                    return scrollViewer;

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UpdaterViewModel viewModel)
                viewModel.SetCloseAction(this.Close);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is UpdaterViewModel viewModel)
                viewModel.CloseWindowAction();
        }
    }
}
