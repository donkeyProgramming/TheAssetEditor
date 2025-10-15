using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer.Table
{
    public class ViewerStateGroupTableService(IEventHub eventHub, IAudioEditorStateService audioEditorStateService) : IViewerTableService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
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
            var stateGroupName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateGroup = _audioEditorStateService.AudioProject.GetStateGroup(stateGroupName);
            foreach (var state in stateGroup.States)
            {
                var row = table.NewRow();
                row[columnHeader] = state.Name;
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }
    }
}
