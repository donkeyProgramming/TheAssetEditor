using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.ViewModels;

namespace Editors.Audio.AudioEditor
{
    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        public AudioEditorView()
        {
            InitializeComponent();

            AudioProjectViewerDataGrid.SelectionChanged += DataGrid_SelectionChanged;
        }

        
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedItems = dataGrid.SelectedItems;

            ViewModel.IsPlayAudioButtonEnabled = false;

            if (ViewModel != null && selectedItems != null)
            {
                var selectedDataGridRows = ViewModel.SelectedDataGridRows;
                selectedDataGridRows.Clear();

                foreach (var item in selectedItems.OfType<Dictionary<string, object>>())
                    selectedDataGridRows.Add(item);

                if (selectedDataGridRows.Count == 1)
                {
                    if (selectedDataGridRows[0].TryGetValue("AudioFiles", out var audioFilesObj) && audioFilesObj is List<string> audioFiles && audioFiles.Any())
                        ViewModel.IsPlayAudioButtonEnabled = true;
                }
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
                ViewModel.OnSelectedAudioProjectEventChanged(e.NewValue);
        }
    }
}
