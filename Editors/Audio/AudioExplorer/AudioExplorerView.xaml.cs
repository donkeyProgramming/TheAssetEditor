using System.Windows.Controls;
using System.Windows.Input;
using Editors.Audio.AudioExplorer;

namespace Audio.AudioExplorer
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
