using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.Data;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerView : UserControl
    {
        public AudioProjectViewerViewModel ViewModel => DataContext as AudioProjectViewerViewModel;

        public AudioProjectViewerView()
        {
            InitializeComponent();
            Loaded += AudioProjectViewerView_Loaded;
        }

        private void AudioProjectViewerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var dataGridTag = ViewModel?.AudioProjectViewerDataGridTag;
            var dataGrid = DataGridHelpers.GetDataGridByTag(dataGridTag);
            dataGrid.SelectionChanged += AudioEditorDataGrid_SelectionChanged;
        }

        // Detects when a row in the DataGrid is selected
        private void AudioEditorDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedItems = dataGrid.SelectedItems;
            if (ViewModel != null && selectedItems != null)
                ViewModel.OnDataGridSelectionChanged(selectedItems);
        }
    }
}
