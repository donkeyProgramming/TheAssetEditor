using System;
using System.Windows;
using Editors.Audio.AudioProjectConverter;

namespace Editors.Audio.AudioProjectMerger
{
    public partial class AudioProjectMergerWindow : Window
    {
        public AudioProjectMergerWindow()
        {
            InitializeComponent();
            Loaded += AudioProjectConverterWindowLoaded;
        }

        private void AudioProjectConverterWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AudioProjectMergerViewModel viewModel)
                viewModel.SetCloseAction(Close);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is AudioProjectMergerViewModel viewModel)
                viewModel.CloseWindowAction();
        }
    }
}
