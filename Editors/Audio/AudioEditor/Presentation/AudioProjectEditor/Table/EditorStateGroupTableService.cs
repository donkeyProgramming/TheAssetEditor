using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table
{
    public class EditorStateGroupTableService(IEventHub eventHub) : IEditorTableService
    {
        private readonly IEventHub _eventHub = eventHub;

        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.StateGroup;

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
                _eventHub.Publish(new EditorTableColumnAddRequestedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var columnWidth = 1.0;

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateEditableTextBoxTemplate(_eventHub, columnName);
                _eventHub.Publish(new EditorDataGridColumnAddRequestedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var columnHeader = TableInfo.StateColumnName;
            var row = table.NewRow();
            row[columnHeader] = string.Empty;
            _eventHub.Publish(new EditorTableRowAddRequestedEvent(row));
        }
    }
}
