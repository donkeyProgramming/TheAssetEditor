using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class ViewerDialogueEventDataGridService : IDataGridService
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Viewer;
        public NodeType NodeType => NodeType.DialogueEvent;

        public ViewerDialogueEventDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void LoadDataGrid(DataTable table)
        {
            SetTableSchema();
            ConfigureDataGrid();
            SetInitialDataGridData(table);
        }

        public void SetTableSchema()
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var column = new DataColumn(columnHeader, typeof(string));
                _eventHub.Publish(new AddViewerTableColumnEvent(column));
            }
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectViewerDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);
            _eventHub.Publish(new SetDataGridContextMenuEvent(dataGrid));

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);
            
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var column = DataGridTemplates.CreateColumnTemplate(columnHeader, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnHeader);
                dataGrid.Columns.Add(column);
            }
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var statePath in dialogueEvent.StatePaths)
                ProcessStatePathData(table, stateGroupsWithQualifiers, statePath);
        }

        private void ProcessStatePathData(DataTable table, Dictionary<string, string> stateGroupsWithQualifiers, StatePath statePath)
        {
            var row = table.NewRow();

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                if (node != null)
                    row[columnHeader] = node.State.Name;
                else
                    row[columnHeader] = string.Empty;
            }

            _eventHub.Publish(new AddViewerTableRowEvent(row));
        }
    }
}
