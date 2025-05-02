using System.Windows;

namespace Editors.Audio.AudioProjectConverter
{
    public partial class AudioProjectConverterWindow : Window
    {
        public AudioProjectConverterWindow()
        {
            InitializeComponent();
            Loaded += AudioProjectConverterWindow_Loaded;
        }

        private void AudioProjectConverterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AudioProjectConverterViewModel viewModel)
                viewModel.SetCloseAction(this.Close);
        }
    }
}
