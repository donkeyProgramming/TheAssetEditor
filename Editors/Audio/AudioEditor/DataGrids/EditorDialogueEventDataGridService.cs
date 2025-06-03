using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class EditorDialogueEventDataGridService : IDataGridService
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Editor;
        public NodeType NodeType => NodeType.DialogueEvent;

        public EditorDialogueEventDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository)
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
                _eventHub.Publish(new AddEditorTableColumnEvent(column));
            }
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var states = DataGridHelpers.GetStatesForStateGroupColumn(_audioEditorService.AudioEditorViewModel, _audioRepository, _audioEditorService, stateGroupWithQualifier.Value);
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var column = DataGridTemplates.CreateColumnTemplate(columnHeader, columnWidth);
                column.CellTemplate = DataGridTemplates.CreateStatesComboBoxTemplate(_eventHub, columnHeader, states);
                dataGrid.Columns.Add(column);
            }
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var row = table.NewRow();

            var stateGroupsWithAnyState = _audioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = AudioProjectHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEvent.Name, DataGridHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    row[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            _eventHub.Publish(new AddEditorTableRowEvent(row));
        }
    }
}
