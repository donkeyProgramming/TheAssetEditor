using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class ActionEventDataGridService : IAudioProjectViewerDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public ActionEventDataGridService(IAudioEditorService audioEditorService)
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

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var table = _audioEditorService.GetViewerDataGrid();
            if (!table.Columns.Contains(DataGridConfiguration.EventNameColumn))
                table.Columns.Add(new DataColumn(DataGridConfiguration.EventNameColumn, typeof(string)));

            var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.EventNameColumn, columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetDataGridData()
        {
            var table = _audioEditorService.GetViewerDataGrid();

            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var row = table.NewRow();
                row[DataGridConfiguration.EventNameColumn] = actionEvent.Name;
                _audioEditorService.GetViewerDataGrid().Rows.Add(row);
            }
        }

        public void InsertDataGridRow()
        {
            var table = _audioEditorService.GetEditorDataGrid();

            var editorRow = _audioEditorService.GetEditorDataGrid().Rows[0];
            var value = AudioProjectHelpers.GetActionEventNameFromRow(editorRow);

            var row = table.NewRow();
            row[DataGridConfiguration.EventNameColumn] = value;
            DataGridHelpers.InsertRowAlphabetically(_audioEditorService.GetViewerDataGrid(), row);
        }
    }
}
