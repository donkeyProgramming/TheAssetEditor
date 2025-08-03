using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.Table
{
    public class ViewerStateGroupDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService) : IViewerTableService
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
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
                _eventHub.Publish(new ViewerTableColumnAddRequestedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var columnWidth = 1.0;

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnName);
                _eventHub.Publish(new ViewerDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var columnHeader = TableInfo.StateColumnName;
            var stateGroupName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var stateGroup = _audioEditorService.AudioProject.GetStateGroup(stateGroupName);
            foreach (var state in stateGroup.States)
            {
                var row = table.NewRow();
                row[columnHeader] = state.Name;
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }
    }
}
