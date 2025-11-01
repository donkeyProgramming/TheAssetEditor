using System.Windows;

namespace Editors.Audio.AudioEditor.Presentation.NewAudioProject
{
    public partial class NewAudioProjectWindow : Window
    {
        public NewAudioProjectWindow()
        {
            InitializeComponent();
            Loaded += NewAudioProjectWindow_Loaded;
        }

        private void NewAudioProjectWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is NewAudioProjectViewModel viewModel)
                viewModel.SetCloseAction(this.Close);
        }
    }
}
