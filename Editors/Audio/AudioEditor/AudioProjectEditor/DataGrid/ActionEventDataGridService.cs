using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.GameSettings.Warhammer3;
using static Editors.Audio.GameSettings.Warhammer3.ActionTypes;

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

            var actionTypes = Enum.GetValues<Wh3ActionType>().Select(actionType => actionType.ToString()).ToList();
            var actionTypeColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.ActionTypeColumn, 75, DataGridColumnType.ReadOnlyComboBox, actionTypes, useAbsoluteWidth: true);
            dataGrid.Columns.Add(actionTypeColumn);

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
                var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridConfiguration.EventNameColumn, columnWidth, DataGridColumnType.EditableTextBox);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void InitialiseDataGridData()
        {
            var rowData = new Dictionary<string, string>
            {
                { DataGridConfiguration.ActionTypeColumn, Wh3ActionType.Play.ToString() }, // Use Play as the default
                { DataGridConfiguration.EventNameColumn, string.Empty }
            };
            _audioEditorService.GetEditorDataGrid().Add(rowData);
        }

        public void SetDataGridData()
        {
            var selectedRow = _audioEditorService.GetSelectedViewerRows()[0];
            var eventValue = selectedRow[DataGridConfiguration.EventNameColumn];

            // Split the eventColumnValue into two parts using the first underscore as the delimiter
            var parts = eventValue.Split(['_'], 2);
            var actionTypeValue = parts[0];
            var eventNameValue = parts.Length > 1 ? parts[1] : string.Empty;

            var rowData = new Dictionary<string, string>
            {
                { DataGridConfiguration.ActionTypeColumn, actionTypeValue },
                { DataGridConfiguration.EventNameColumn, eventNameValue }
            };

            _audioEditorService.GetEditorDataGrid().Add(rowData);
        }
    }
}
