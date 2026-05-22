using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editors.Audio.AudioEditor.Presentation.Shared.Controls;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
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
            if (e.Data.GetDataPresent(typeof(IEnumerable<IMultiSelectTreeNode>)))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnListViewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(IEnumerable<IMultiSelectTreeNode>)))
                return;

            if (e.Data.GetData(typeof(IEnumerable<IMultiSelectTreeNode>)) is not IEnumerable<IMultiSelectTreeNode> droppedMultiSelectTreeNodes)
                return;

            var droppedAudioFilesTreeNodes = droppedMultiSelectTreeNodes
                .OfType<AudioFilesTreeNode>()
                .Where(node => node.Type == AudioFilesTreeNodeType.WavFile)
                .ToList();
            if (droppedAudioFilesTreeNodes.Count == 0)
                return;

            ViewModel.SetAudioFilesViaDrop(droppedAudioFilesTreeNodes);
        }

        private void OnAudioFilesListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AudioFilesListView.SelectedItem is AudioFile audioFile)
                ViewModel.PlayWav(audioFile);
        }

        private void OnAudioFilesListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedAudioFiles = AudioFilesListView.SelectedItems
                .Cast<AudioFile>()
                .Where(audioFile => audioFile != null)
                .ToList();
            ViewModel.OnSelectedAudioFilesChanged(selectedAudioFiles);
        }
    }
}
