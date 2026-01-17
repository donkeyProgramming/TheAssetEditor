using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer
{
    public partial class AudioProjectViewerView : UserControl
    {
        public AudioProjectViewerViewModel ViewModel => DataContext as AudioProjectViewerViewModel;

        public AudioProjectViewerView()
        {
            InitializeComponent();
        }

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var dataGrid = (DataGrid)sender;
            ViewModel.SelectedRows = dataGrid.SelectedItems
                .Cast<DataRowView>()
                .Select(dataRowView => dataRowView.Row)
                .ToList();
        }
    }
}
