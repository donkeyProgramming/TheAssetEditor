using System.Windows;
using System.Windows.Controls;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.AudioEditor.Presentation.Shared;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer
{
    public partial class AudioProjectExplorerView : UserControl
    {
        public AudioProjectExplorerViewModel ViewModel => DataContext as AudioProjectExplorerViewModel;

        public AudioProjectExplorerView()
        {
            InitializeComponent();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is AudioProjectTreeNode selectedNode)
                ViewModel.SelectedNode = selectedNode;
        }

        private void OnWatermarkComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DialogueEventTypeFilterComboBox?.SelectedItem is Wh3DialogueEventType type && type == Wh3DialogueEventType.TypeShowAll)
            {
                DialogueEventTypeFilterComboBox.SelectedItem = null;
                ViewModel.ResetDialogueEventTypeFilterComboBoxSelectedItem();
            }

            if (DialogueEventProfileFilterComboBox?.SelectedItem is Wh3DialogueEventUnitProfile target && target == Wh3DialogueEventUnitProfile.ProfileShowAll)
            {
                DialogueEventProfileFilterComboBox.SelectedItem = null;
                ViewModel.ResetDialogueEventProfileFilterComboBoxSelectedItem();
            }
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            FilterTextBoxItem.Focus();
        }
    }
}
