using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.Table
{
    public class ViewerActionEventDataGridService(IEventHub eventHub, IAudioEditorStateService audioEditorStateService) : IViewerTableService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventSoundBank;

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
            var columnName = TableInfo.EventColumnName;
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
            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth, isReadOnly: true);
                column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnName);
                _eventHub.Publish(new ViewerDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var soundBankName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var row = table.NewRow();
                row[TableInfo.EventColumnName] = actionEvent.Name;
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }
    }
}
