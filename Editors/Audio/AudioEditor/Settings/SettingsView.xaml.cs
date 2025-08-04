using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editors.Audio.AudioEditor.AudioFilesExplorer;

namespace Editors.Audio.AudioEditor.Settings
{
    public partial class SettingsView : UserControl
    {
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

                if (DataContext is SettingsViewModel viewModel)
                {
                    var audioFiles = new ObservableCollection<AudioFile>();
                    foreach (var wavFile in droppedNodes)
                    {
                        audioFiles.Add(new AudioFile
                        {
                            FileName = wavFile.FileName,
                            FilePath = wavFile.FilePath
                        });
                    }
                    viewModel.SetAudioFilesViaDrop(audioFiles);
                }
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
    }
}
