using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.Storage;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.Table
{
    public class EditorDialogueEventDataGridService(
        IEventHub eventHub,
        IAudioEditorService audioEditorService,
        IAudioRepository audioRepository) : IEditorTableService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IAudioRepository _audioRepository = audioRepository;

        public AudioProjectExplorerTreeNodeType NodeType => AudioProjectExplorerTreeNodeType.DialogueEvent;

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
            var dialogueEventName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEventName];
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
                _eventHub.Publish(new EditorTableColumnAddedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var dialogueEventName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEventName].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var columnName in schema)
            {
                var stateGroupNameWithQualifier = TableHelpers.DeduplicateUnderscores(columnName);
                var stateGroupName = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, stateGroupNameWithQualifier);

                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                var states = TableHelpers.GetStatesForStateGroupColumn(_audioEditorService, _audioRepository, stateGroupName);
                column.CellTemplate = DataGridTemplates.CreateStatesComboBoxTemplate(_eventHub, columnName, states);
                _eventHub.Publish(new EditorDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var row = table.NewRow();

            var stateGroupsWithAnyState = _audioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var selectedAudioProjectExplorerNodeName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(selectedAudioProjectExplorerNodeName);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                var stateGroup = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEvent.Name, TableHelpers.DeduplicateUnderscores(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    row[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            _eventHub.Publish(new EditorTableRowAddedEvent(row));
        }
    }
}
