using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editors.Audio.AudioEditor.Presentation.Shared;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer
{
    public partial class AudioFilesExplorerView : UserControl
    {
        public AudioFilesExplorerViewModel ViewModel => DataContext as AudioFilesExplorerViewModel;

        public AudioFilesExplorerView()
        {
            InitializeComponent();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e) => FilterTextBoxItem.Focus();

        private void OnNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source is not TreeViewItem)
                source = VisualTreeHelper.GetParent(source);

            var treeViewItem = source as TreeViewItem;
            if (treeViewItem?.DataContext is not AudioFilesTreeNode node)
                return;

            if (node.Type == AudioFilesTreeNodeType.WavFile)
            {
                ViewModel.PlayWav();
                e.Handled = true;
            }
        }
    }
}
