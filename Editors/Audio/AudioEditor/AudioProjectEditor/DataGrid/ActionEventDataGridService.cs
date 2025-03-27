using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
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
            var eventName = string.Empty;

            var selectedNode = _audioEditorService.GetSelectedExplorerNode();
            if (selectedNode.Name != SoundBanks.MoviesDisplayString)
                eventName = "Play_";

            var rowData = new Dictionary<string, string>
            {
                { DataGridConfiguration.EventNameColumn, eventName }
            };
            _audioEditorService.GetEditorDataGrid().Add(rowData);
        }

        public void SetDataGridData()
        {
            var selectedRow = _audioEditorService.GetSelectedViewerRows()[0];
            var eventValue = selectedRow[DataGridConfiguration.EventNameColumn];

            var rowData = new Dictionary<string, string>
            {
                { DataGridConfiguration.EventNameColumn, eventValue }
            };

            _audioEditorService.GetEditorDataGrid().Add(rowData);
        }
    }
}
