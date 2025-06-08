using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.GameSettings.Warhammer3;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class EditorActionEventDataGridService : IDataGridService
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Editor;
        public NodeType NodeType => NodeType.ActionEventSoundBank;

        public EditorActionEventDataGridService(IUiCommandFactory uiCommandFactory, IEventHub eventHub, IAudioEditorService audioEditorService)
        {
            _uiCommandFactory = uiCommandFactory;
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid(DataTable table)
        {
            SetTableSchema();
            ConfigureDataGrid();
            SetInitialDataGridData(table);
        }

        public void SetTableSchema()
        {
            var columnHeader = DataGridTemplates.EventColumn;
            var column = new DataColumn(columnHeader, typeof(string));
            _eventHub.Publish(new AddEditorTableColumnEvent(column));
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectEditorDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var columnHeader = DataGridTemplates.EventColumn;
            var columnsCount = 1;
            var columnWidth = 1.0 / columnsCount;

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.Name == SoundBanks.MoviesDisplayString)
            {
                var fileSelectColumnHeader = DataGridTemplates.BrowseMovieColumn;
                var fileSelectColumn = DataGridTemplates.CreateColumnTemplate(fileSelectColumnHeader, 85, useAbsoluteWidth: true);
                fileSelectColumn.CellTemplate = DataGridTemplates.CreateFileSelectButtonCellTemplate(_uiCommandFactory);
                dataGrid.Columns.Add(fileSelectColumn);

                var eventColumn = DataGridTemplates.CreateColumnTemplate(columnHeader, columnWidth, isReadOnly: true);
                eventColumn.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnHeader);
                dataGrid.Columns.Add(eventColumn);
            }
            else
            {
                var eventColumn = DataGridTemplates.CreateColumnTemplate(columnHeader, columnWidth, isReadOnly: true);
                eventColumn.CellTemplate = DataGridTemplates.CreateEditableEventTextBoxTemplate(_eventHub, columnHeader);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void SetInitialDataGridData(DataTable editorTable)
        {
            var eventName = string.Empty;

            var selectedNode = _audioEditorService.SelectedExplorerNode;
            if (selectedNode.Name != SoundBanks.MoviesDisplayString)
                eventName = "Play_";

            var row = editorTable.NewRow();
            row[DataGridTemplates.EventColumn] = eventName;

            _eventHub.Publish(new AddEditorTableRowEvent(row));
        }
    }
}
