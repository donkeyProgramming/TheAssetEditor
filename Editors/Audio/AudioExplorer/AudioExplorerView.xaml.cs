using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Editors.Audio.AudioExplorer
{
    public partial class AudioExplorerView : UserControl
    {
        public AudioExplorerView()
        {
            InitializeComponent();
        }

        private void OnNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not System.Windows.DependencyObject source ||
                FindAncestor<TreeViewItem>(source) is not TreeViewItem treeViewItem ||
                treeViewItem.DataContext is not HircTreeNode node)
                return;

            if (node.IsExpanded)
                node.IsExpanded = false;
            else
                AudioExplorerViewModel.RunDepthFirstSearchToSound(node);

            e.Handled = true;
        }

        private static T FindAncestor<T>(System.Windows.DependencyObject source)
            where T : System.Windows.DependencyObject
        {
            for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is T ancestor)
                    return ancestor;
            }

            return null;
        }

        private void OnNodeExpanded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!ReferenceEquals(sender, e.OriginalSource) ||
                sender is not TreeViewItem treeViewItem ||
                treeViewItem.DataContext is not HircTreeNode node ||
                DataContext is not AudioExplorerViewModel viewModel)
                return;

            viewModel.PreloadWaveformsForNodes(node.Children);
        }
    }
}
