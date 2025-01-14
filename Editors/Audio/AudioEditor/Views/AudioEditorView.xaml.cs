using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.ViewModels;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;

namespace Editors.Audio.AudioEditor.Views
{
    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        public AudioEditorView()
        {
            InitializeComponent();

            Loaded += AudioEditorView_Loaded;
        }

        private void AudioEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            var dataGridTag = ViewModel?.AudioProjectEditorFullDataGridTag;
            var dataGrid = DataGridHelpers.GetDataGridByTag(dataGridTag);
            dataGrid.SelectionChanged += DataGrid_SelectionChanged;
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedItems = dataGrid.SelectedItems;
            if (ViewModel != null && selectedItems != null)
                ViewModel.OnDataGridSelectionChanged(selectedItems);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
                ViewModel.OnSelectedAudioProjectTreeViewItemChanged(e.NewValue);
        }
    }
}
