using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class StateGroupDataGridService : IAudioProjectViewerDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public StateGroupDataGridService(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid()
        {
            ConfigureDataGrid();
            SetDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);

            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            var table = _audioEditorService.GetViewerDataGrid();
            if (!table.Columns.Contains(columnHeader))
                table.Columns.Add(new DataColumn(columnHeader, typeof(string)));

            var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, columnHeader, 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData()
        {
            var table = _audioEditorService.GetViewerDataGrid();

            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            foreach (var state in stateGroup.States)
            {
                var row = table.NewRow();
                row[columnHeader] = state.Name;
                table.Rows.Add(row);
            }
        }

        public void InsertDataGridRow()
        {
            DataGridHelpers.InsertRowAlphabetically(_audioEditorService.GetViewerDataGrid(), _audioEditorService.GetEditorDataGrid().Rows[0]);
        }
    }
}
