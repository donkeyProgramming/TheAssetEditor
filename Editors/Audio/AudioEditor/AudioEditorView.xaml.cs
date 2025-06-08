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
            PreviewKeyDown += OnPreviewKeyDown;
        }

        // This is here rather than the Viewer because the viewer only recognises key presses when you're clicked on the grid itself whereas this recognises them anywhere 
        private void OnPreviewKeyDown(object sender, KeyEventArgs e) => ViewModel.OnPreviewKeyDown(e);
    }
}
