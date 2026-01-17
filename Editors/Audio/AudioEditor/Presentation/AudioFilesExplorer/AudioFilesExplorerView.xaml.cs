using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;

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

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source is not TreeViewItem)
                source = VisualTreeHelper.GetParent(source);

            if (source is not TreeViewItem treeViewItem)
                return;

            if (treeViewItem.DataContext is not AudioFilesTreeNode node || node.Type != AudioFilesTreeNodeType.WavFile)
                return;

            ViewModel.PlayWav();
            e.Handled = true;
        }
    }
}
