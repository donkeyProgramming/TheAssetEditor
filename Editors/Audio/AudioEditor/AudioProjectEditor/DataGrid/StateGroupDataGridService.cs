using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class StateGroupDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public StateGroupDataGridService(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid()
        {
            ConfigureDataGrid();
            InitialiseDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            var table = _audioEditorService.GetEditorDataGrid();
            if (!table.Columns.Contains(columnHeader))
                table.Columns.Add(new DataColumn(columnHeader, typeof(string)));

            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, columnHeader, 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void InitialiseDataGridData()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            var table = _audioEditorService.GetEditorDataGrid();
            var row = table.NewRow();
            row[columnHeader] = string.Empty;
            table.Rows.Add(row);
        }

        public void SetDataGridData()
        {
            _audioEditorService.GetEditorDataGrid().Rows.Add(_audioEditorService.GetSelectedViewerRows()[0]);
        }
    }
}
