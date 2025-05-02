using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class DialogueEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public DialogueEventDataGridService(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void LoadDataGrid()
        {
            ConfigureDataGrid();
            InitialiseDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var table = _audioEditorService.GetEditorDataGrid();

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);

                if (!table.Columns.Contains(columnHeader))
                    table.Columns.Add(new DataColumn(columnHeader, typeof(string)));

                var states = DataGridHelpers.GetStatesForStateGroupColumn(_audioEditorService.AudioEditorViewModel, _audioRepository, _audioEditorService, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void InitialiseDataGridData()
        {
            var table = _audioEditorService.GetEditorDataGrid();
            var row = table.NewRow();

            var stateGroupsWithAnyState = _audioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = _audioRepository.GetStateGroupFromStateGroupWithQualifier(dialogueEvent.Name, DataGridHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    row[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            table.Rows.Add(row);
        }

        public void SetDataGridData()
        {
            var row = _audioEditorService.GetSelectedViewerRows()[0];
            var table = _audioEditorService.GetEditorDataGrid();
            table.ImportRow(row);
        }
    }
}
