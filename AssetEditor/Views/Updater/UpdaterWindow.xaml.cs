using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AssetEditor.ViewModels;
using MdXaml;

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

        private void OnMarkdownScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MarkdownScrollViewer markdownScrollViewer)
                return;

            var flowDocument = markdownScrollViewer.Document;
            if (flowDocument == null)
                return;

            // We format the list manually as the Controls.xaml doesn't let you set MarkerStyle
            // and the spacing before the list is too big but changing the margin of paragraph
            // in Controls.xaml changes all blocks rather than just the block for lists.
            FormatList(flowDocument);
        }

        private static void FormatList(FlowDocument flowDocument)
        {
            var currentBlock = flowDocument.Blocks.FirstBlock;

            while (currentBlock != null)
            {
                var nextBlock = currentBlock.NextBlock;

                if (currentBlock is List list)
                {
                    list.MarkerStyle = TextMarkerStyle.Disc;
                    list.Margin = new Thickness(0);
                    list.Padding = new Thickness(list.Padding.Left, 0, list.Padding.Right, 0);

                    if (list.ListItems.FirstListItem?.Blocks.FirstBlock is Paragraph firstParagraph)
                        firstParagraph.Margin = new Thickness(firstParagraph.Margin.Left, 10, firstParagraph.Margin.Right, firstParagraph.Margin.Bottom);
                }

                if (currentBlock is Paragraph paragraph && nextBlock is List)
                    paragraph.Margin = new Thickness(paragraph.Margin.Left, paragraph.Margin.Top, paragraph.Margin.Right, 0);

                currentBlock = nextBlock;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is UpdaterViewModel viewModel)
                viewModel.CloseWindowAction();
        }
    }
}
