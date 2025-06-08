using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectViewer
{
    public partial class AudioProjectViewerView : UserControl
    {
        public AudioProjectViewerViewModel ViewModel => DataContext as AudioProjectViewerViewModel;

        public AudioProjectViewerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var dataGridTag = ViewModel?.AudioProjectViewerDataGridTag;
            var dataGrid = DataGridHelpers.GetDataGridFromTag(dataGridTag);
            dataGrid.SelectionChanged += OnDataGridSelectionChanged;
        }

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedViewerRows = dataGrid.SelectedItems.Cast<DataRowView>().Select(dataRowView => dataRowView.Row).ToList();
            if (ViewModel != null && selectedViewerRows != null)
                ViewModel.SelectedRows = selectedViewerRows;
        }
    }
}
