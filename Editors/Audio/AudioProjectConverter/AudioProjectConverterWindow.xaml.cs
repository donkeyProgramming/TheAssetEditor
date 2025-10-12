using System;
using System.Windows;

namespace Editors.Audio.AudioProjectConverter
{
    public partial class AudioProjectConverterWindow : Window
    {
        public AudioProjectConverterWindow()
        {
            InitializeComponent();
            Loaded += AudioProjectConverterWindowLoaded;
        }

        private void AudioProjectConverterWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AudioProjectConverterViewModel viewModel)
                viewModel.SetCloseAction(Close);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is AudioProjectConverterViewModel viewModel)
                viewModel.CloseWindowAction();
        }
    }
}
