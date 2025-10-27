using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.AudioEditor.Presentation.Settings
{
    public partial class SettingsView : UserControl
    {
        public SettingsViewModel ViewModel => DataContext as SettingsViewModel;

        public SettingsView()
        {
            InitializeComponent();
        }

        private void OnListViewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<AudioFilesTreeNode>)))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnListViewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<AudioFilesTreeNode>)))
            {
                if (e.Data.GetData(typeof(IEnumerable<AudioFilesTreeNode>)) is not IEnumerable<AudioFilesTreeNode> droppedNodes)
                    return;

                ViewModel.SetAudioFilesViaDrop(droppedNodes);
            }
        }

        private void OnAudioFileDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                if (AudioFilesListView.SelectedItem is AudioFile audioFile)
                    viewModel.PlayWav(audioFile);
            }
        }

        private void OnAudioFilesListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (ViewModel.ShowSettingsFromAudioProjectViewer)
                    return;

                var selectedAudioFiles = AudioFilesListView.SelectedItems
                    .Cast<AudioFile>()
                    .Where(audioFile => audioFile != null)
                    .ToList();

                if (selectedAudioFiles.Count == 0)
                    return;

                ViewModel.RemoveAudioFiles(selectedAudioFiles);

                e.Handled = true;
            }
        }
    }
}
