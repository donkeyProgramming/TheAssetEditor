using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class EditorStateGroupDataGridService : IDataGridService
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Editor;
        public NodeType NodeType => NodeType.StateGroup;

        public EditorStateGroupDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService)
        {
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
            var columnHeader = DataGridTemplates.StateColumn;
            var column = new DataColumn(columnHeader, typeof(string));
            _eventHub.Publish(new AddEditorTableColumnEvent(column));
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectEditorDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var columnHeader = DataGridTemplates.StateColumn;
            var column = DataGridTemplates.CreateColumnTemplate(columnHeader, 1.0);
            column.CellTemplate = DataGridTemplates.CreateEditableTextBoxTemplate(_eventHub, columnHeader);
            dataGrid.Columns.Add(column);
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var columnHeader = DataGridTemplates.StateColumn;
            var row = table.NewRow();
            row[columnHeader] = string.Empty;
            _eventHub.Publish(new AddEditorTableRowEvent(row));
        }
    }
}
