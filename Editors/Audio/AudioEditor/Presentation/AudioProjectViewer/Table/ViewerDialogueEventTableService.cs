using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer.Table
{
    public class ViewerDialogueEventTableService(
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
                _eventHub.Publish(new ViewerTableColumnAddRequestedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateGroupsCount = _audioRepository.StateGroupsByDialogueEvent[dialogueEventName].Count;
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
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupByStateGroupByDialogueEvent[dialogueEvent.Name];
            var orderedStateGroupsWithQualifiers = stateGroupsWithQualifiers.ToList();

            foreach (var statePath in dialogueEvent.StatePaths)
            {
                var row = table.NewRow();

                // We get the State Group by occurance as a State Group can appear multiple times in a State Path so we want to access the right one
                var occurrenceIndexByStateGroupName = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var stateGroupWithQualifier in orderedStateGroupsWithQualifiers)
                {
                    var columnHeader = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                    var stateGroupName = stateGroupWithQualifier.Value;

                    if (!occurrenceIndexByStateGroupName.TryGetValue(stateGroupName, out var currentOccurrenceIndex))
                        currentOccurrenceIndex = 0;
                    else
                        currentOccurrenceIndex += 1;

                    occurrenceIndexByStateGroupName[stateGroupName] = currentOccurrenceIndex;

                    var nodeForThisOccurrence = statePath.Nodes
                        .Where(candidateNode => candidateNode.StateGroup.Name == stateGroupName)
                        .Skip(currentOccurrenceIndex)
                        .FirstOrDefault();

                    if (nodeForThisOccurrence != null)
                        row[columnHeader] = nodeForThisOccurrence.State.Name;
                    else
                        row[columnHeader] = string.Empty;
                }

                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }

    }
}
