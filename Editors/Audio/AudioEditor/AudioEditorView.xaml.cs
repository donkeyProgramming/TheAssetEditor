using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        // This is here rather than the Audio Project Viewer because the the Viewer DataGrid only recognises key presses when
        // you're focussed on the DataGrid and if you delete an item it loses focus whereas this recognises them anywhere so.
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsPressedWithFocusOnAudioFilesListView(e.OriginalSource as DependencyObject))
                return;

            ViewModel.OnPreviewKeyDown(e);
        }

        private static bool IsPressedWithFocusOnAudioFilesListView(DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                if (current is FrameworkElement frameworkElement && frameworkElement.Name == "AudioFilesListView")
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}
