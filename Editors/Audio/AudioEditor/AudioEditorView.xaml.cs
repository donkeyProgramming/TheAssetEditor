using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editors.Audio.AudioEditor.Data;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        public AudioEditorView()
        {
            InitializeComponent();

            Loaded += AudioEditorView_Loaded;
            PreviewKeyDown += AudioEditorView_PreviewKeyDown;
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

        private void AudioEditorView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    ViewModel?.CopyRows();
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    ViewModel?.PasteRows();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Delete)
            {
                ViewModel?.RemoveAudioProjectEditorFullDataGridRow();
                e.Handled = true;
            }
        }

    }
}
