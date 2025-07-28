using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.Table
{
    public class EditorStateGroupDataGridService(IEventHub eventHub) : IEditorTableService
    {
        private readonly IEventHub _eventHub = eventHub;

        public AudioProjectExplorerTreeNodeType NodeType => AudioProjectExplorerTreeNodeType.StateGroup;

        public void Load(DataTable table)
        {
            var schema = DefineSchema();
            ConfigureTable(schema);
            ConfigureDataGrid(schema);
            InitialiseTable(table);
        }

        public List<string> DefineSchema()
        {
            var schema = new List<string>();
            var columnName = TableInfo.StateColumnName;
            schema.Add(columnName);
            return schema;
        }

        public void ConfigureTable(List<string> schema)
        {
            foreach (var columnName in schema)
            {
                var column = new DataColumn(columnName, typeof(string));
                _eventHub.Publish(new EditorTableColumnAddedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var columnWidth = 1.0;

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateEditableTextBoxTemplate(_eventHub, columnName);
                _eventHub.Publish(new EditorDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var columnHeader = TableInfo.StateColumnName;
            var row = table.NewRow();
            row[columnHeader] = string.Empty;
            _eventHub.Publish(new EditorTableRowAddedEvent(row));
        }
    }
}
