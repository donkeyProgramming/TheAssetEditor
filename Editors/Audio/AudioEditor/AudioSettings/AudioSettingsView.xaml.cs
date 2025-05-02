using System.Collections.Generic;
using System.IO;
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

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<TreeNode>)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IEnumerable<TreeNode>)))
            {
                var droppedNodes = e.Data.GetData(typeof(IEnumerable<TreeNode>)) as IEnumerable<TreeNode>;
                if (droppedNodes == null) return;

                if (DataContext is AudioSettingsViewModel viewModel)
                {
                    viewModel.AudioFiles.Clear();

                    foreach (var node in droppedNodes)
                    {
                        if (node.NodeType == NodeType.WavFile)
                        {
                            var audioFile = new AudioFile
                            {
                                FileName = Path.GetFileName(node.FilePath),
                                FilePath = node.FilePath
                            };

                            if (!viewModel.AudioFiles.Any(f => f.FilePath == audioFile.FilePath))
                                viewModel.AudioFiles.Add(audioFile);
                        }
                    }
                }
            }
        }
    }
}
