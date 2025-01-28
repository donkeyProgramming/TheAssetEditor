using System.Windows.Controls;
using System.Windows.Input;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        public AudioEditorView()
        {
            InitializeComponent();
            PreviewKeyDown += AudioProjectViewerView_PreviewKeyDown;

        }

        private void AudioProjectViewerView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    ViewModel?.AudioProjectViewerViewModel.CopyRows();
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    ViewModel?.AudioProjectViewerViewModel.PasteRows();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Delete)
            {
                ViewModel?.AudioProjectViewerViewModel.RemoveAudioProjectViewerDataGridRow();
                e.Handled = true;
            }
        }
    }
}
