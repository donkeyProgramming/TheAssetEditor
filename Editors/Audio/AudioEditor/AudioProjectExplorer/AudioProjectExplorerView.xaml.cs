using System.Windows;
using System.Windows.Controls;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerView : UserControl
    {
        public AudioProjectExplorerViewModel ViewModel => DataContext as AudioProjectExplorerViewModel;

        public AudioProjectExplorerView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode selectedNode)
                ViewModel.OnSelectedNodeChanged(selectedNode);
        }

        private void WatermarkComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DialogueEventFilterComboBox?.SelectedItem is DialogueEventPreset selectedPreset && selectedPreset == DialogueEventPreset.ShowAll)
                ViewModel.ResetDialogueEventFilterComboBoxSelectedItem(DialogueEventFilterComboBox);
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            FilterTextBoxItem.Focus();
        }
    }
}
