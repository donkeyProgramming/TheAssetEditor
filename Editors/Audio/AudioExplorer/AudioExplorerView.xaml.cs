using System.Windows.Controls;
using System.Windows.Input;

namespace Editors.Audio.AudioExplorer
{
    public partial class AudioExplorerView : UserControl
    {
        public AudioExplorerViewModel ViewModel => DataContext as AudioExplorerViewModel;

        public AudioExplorerView()
        {
            InitializeComponent();
        }

        private void OnNodeDoubleClick(object sender, MouseButtonEventArgs e) => ViewModel.PlaySelectedSoundAction();
    }
}
