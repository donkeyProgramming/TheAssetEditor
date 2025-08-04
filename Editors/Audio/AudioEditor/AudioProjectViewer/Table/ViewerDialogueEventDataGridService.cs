using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.Storage;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.Table
{
    public class ViewerDialogueEventDataGridService(
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService,
        IAudioRepository audioRepository) : IViewerTableService
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
                _eventHub.Publish(new ViewerTableColumnAddRequestedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEventName].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnName);
                _eventHub.Publish(new ViewerDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(_audioEditorStateService.SelectedAudioProjectExplorerNode.Name);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var statePath in dialogueEvent.StatePaths)
            {
                var row = table.NewRow();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var columnHeader = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                    var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                    if (node != null)
                        row[columnHeader] = node.State.Name;
                    else
                        row[columnHeader] = string.Empty;
                }

                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }
    }
}
