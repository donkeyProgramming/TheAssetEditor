using System.Windows.Controls;

namespace Editors.Audio.AudioExplorer
{
    public partial class AudioExplorerView : UserControl
    {
        public AudioExplorerView()
        {
            InitializeComponent();
        }

        private void OnNodeDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is not AudioExplorerViewModel viewModel || viewModel.SelectedNode == null)
                return;

            AudioExplorerViewModel.RunDepthFirstSearchToSound(viewModel.SelectedNode);
            e.Handled = true;
        }
    }
}
