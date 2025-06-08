using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.AudioFilesExplorer;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    public partial class AudioSettingsView : UserControl
    {
        public AudioSettingsView()
        {
            InitializeComponent();
        }

        private void OnListViewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<TreeNode>)))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnListViewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<TreeNode>)))
            {
                if (e.Data.GetData(typeof(IEnumerable<TreeNode>)) is not IEnumerable<TreeNode> droppedNodes)
                    return;

                if (DataContext is AudioSettingsViewModel viewModel)
                {
                    var audioFiles = new ObservableCollection<AudioFile>();
                    foreach (var wavFile in droppedNodes)
                    {
                        audioFiles.Add(new AudioFile
                        {
                            FileName = wavFile.Name,
                            FilePath = wavFile.FilePath
                        });
                    }
                    viewModel.SetAudioFilesViaDrop(audioFiles);
                }
            }
        }
    }
}
