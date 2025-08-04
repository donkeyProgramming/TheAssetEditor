using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public partial class AudioFilesExplorerView : UserControl
    {
        public AudioFilesExplorerView()
        {
            InitializeComponent();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            FilterTextBoxItem.Focus();
        }

        private void OnNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AudioFilesExplorerViewModel viewModel)
                viewModel.PlayWav();
        }
    }
}
