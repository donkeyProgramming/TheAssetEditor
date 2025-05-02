using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.GameSettings.Warhammer3;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class ActionEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public ActionEventDataGridService(IAudioEditorService audioEditorService)
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
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var columnsCount = 1;
            var columnWidth = 1.0 / columnsCount;

            var table = _audioEditorService.GetEditorDataGrid();
            if (!table.Columns.Contains(DataGridConfiguration.EventNameColumn))
                table.Columns.Add(new DataColumn(DataGridConfiguration.EventNameColumn, typeof(string)));

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.Name == SoundBanks.MoviesDisplayString)
            {
                var fileSelectColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.BrowseMovieColumn, 85, DataGridColumnType.FileSelectButton, useAbsoluteWidth: true);
                dataGrid.Columns.Add(fileSelectColumn);

                var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.EventNameColumn, columnWidth, DataGridColumnType.ReadOnlyTextBlock);
                dataGrid.Columns.Add(eventColumn);
            }
            else
            {
                var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.EventNameColumn, columnWidth, DataGridColumnType.EditableEventTextBox);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void InitialiseDataGridData()
        {
            var table = _audioEditorService.GetEditorDataGrid();

            var eventName = string.Empty;
            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.Name != SoundBanks.MoviesDisplayString)
                eventName = "Play_";

            var row = table.NewRow();
            row[DataGridConfiguration.EventNameColumn] = eventName;
            table.Rows.Add(row);
        }

        public void SetDataGridData()
        {
            var table = _audioEditorService.GetEditorDataGrid();

            var selectedRow = _audioEditorService
                .GetSelectedViewerRows()
                .AsEnumerable()
                .FirstOrDefault();

            if (selectedRow == null)
                return;

            var eventValue = selectedRow[DataGridConfiguration.EventNameColumn]?.ToString();

            var row = table.NewRow();
            row[DataGridConfiguration.EventNameColumn] = eventValue;
            table.Rows.Add(row);
        }
    }
}
