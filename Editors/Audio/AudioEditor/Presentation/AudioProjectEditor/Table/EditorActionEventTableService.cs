using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table
{
    public class EditorActionEventTableService(
        IUiCommandFactory uiCommandFactory,
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService) : IEditorTableService
    {
        private readonly IUiCommandFactory _uiCommandFactory = uiCommandFactory;
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventType;

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
            var columnName = TableInformation.ActionEventColumnName;
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
            var columnsCount = 1;
            var columnWidth = 1.0 / columnsCount;

            if (_audioEditorStateService.SelectedAudioProjectExplorerNode.IsMovieActionEvent())
            {
                var fileSelectColumnHeader = TableInformation.BrowseMovieColumnName;
                var fileSelectColumn = DataGridTemplates.CreateColumnTemplate(fileSelectColumnHeader, 85, useAbsoluteWidth: true);
                fileSelectColumn.CellTemplate = DataGridTemplates.CreateFileSelectButtonCellTemplate(_uiCommandFactory);
                _eventHub.Publish(new EditorDataGridColumnAddRequestedEvent(fileSelectColumn));

                foreach (var columnName in schema)
                {
                    // We don't allow editing because the Event name must match the path of the movie
                    var eventColumn = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth, isReadOnly: true);
                    eventColumn.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnName);
                    _eventHub.Publish(new EditorDataGridColumnAddRequestedEvent(eventColumn));
                }
            }
            else
            {
                foreach (var columnName in schema)
                {
                    var eventColumn = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                    eventColumn.CellTemplate = DataGridTemplates.CreateEditableEventTextBoxTemplate(_eventHub, columnName);
                    _eventHub.Publish(new EditorDataGridColumnAddRequestedEvent(eventColumn));
                }
            }
        }

        public void InitialiseTable(DataTable editorTable)
        {
            var eventName = string.Empty;

            if (!_audioEditorStateService.SelectedAudioProjectExplorerNode.IsMovieActionEvent())
                eventName = "Play_";

            var row = editorTable.NewRow();
            row[TableInformation.ActionEventColumnName] = eventName;

            _eventHub.Publish(new EditorTableRowAddRequestedEvent(row));
        }
    }
}
