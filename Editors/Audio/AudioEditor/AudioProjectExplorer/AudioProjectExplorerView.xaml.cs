using System.Windows;
using System.Windows.Controls;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerView : UserControl
    {
        public AudioProjectExplorerViewModel ViewModel => DataContext as AudioProjectExplorerViewModel;

        public AudioProjectExplorerView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
                ViewModel.OnSelectedAudioProjectTreeViewItemChanged(e.NewValue);
        }
    }
}
