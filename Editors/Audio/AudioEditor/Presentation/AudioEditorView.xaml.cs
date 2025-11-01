using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Editors.Audio.AudioEditor.Presentation
{
    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        public AudioEditorView()
        {
            InitializeComponent();
            PreviewKeyDown += OnPreviewKeyDown;
        }

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
