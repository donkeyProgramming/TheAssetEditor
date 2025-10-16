using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer
{
    public partial class AudioProjectViewerView : UserControl
    {
        public AudioProjectViewerViewModel ViewModel => DataContext as AudioProjectViewerViewModel;

        public AudioProjectViewerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e) => ViewModel.OnPreviewKeyDown(e);

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            AudioProjectEditorDataGrid.SelectionChanged += OnDataGridSelectionChanged;
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
