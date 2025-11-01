using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table
{
    public class EditorDialogueEventTableService(
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService,
        IAudioRepository audioRepository) : IEditorTableService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;

        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.DialogueEvent;

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
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupByStateGroupByDialogueEvent[dialogueEventName];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                schema.Add(columnName);
            }
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
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsCount = _audioRepository.StateGroupsByDialogueEvent[dialogueEventName].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var columnName in schema)
            {
                var stateGroupNameWithQualifier = TableHelpers.DeduplicateUnderscores(columnName);
                var stateGroupName = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, stateGroupNameWithQualifier);

                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                var states = TableHelpers.GetStatesForStateGroupColumn(_audioEditorStateService, _audioRepository, stateGroupName);
                column.CellTemplate = DataGridTemplates.CreateStatesComboBoxTemplate(_eventHub, columnName, states);
                _eventHub.Publish(new EditorDataGridColumnAddRequestedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var row = table.NewRow();

            var stateGroupsWithAnyState = _audioRepository.StatesByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var selectedAudioProjectExplorerNodeName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(selectedAudioProjectExplorerNodeName);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                var stateGroup = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEvent.Name, TableHelpers.DeduplicateUnderscores(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    row[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            _eventHub.Publish(new EditorTableRowAddRequestedEvent(row));
        }
    }
}
