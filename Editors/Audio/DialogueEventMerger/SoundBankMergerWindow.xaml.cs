using System;
using System.Windows;

namespace Editors.Audio.DialogueEventMerger
{
    public partial class DialogueEventMergerWindow : Window
    {
        public DialogueEventMergerWindow()
        {
            InitializeComponent();
            Loaded += DialogueEventMergerWindowLoaded;
        }

        private void DialogueEventMergerWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DialogueEventMergerViewModel viewModel)
                viewModel.SetCloseAction(Close);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is DialogueEventMergerViewModel viewModel)
                viewModel.CloseWindowAction();
        }
    }
}
