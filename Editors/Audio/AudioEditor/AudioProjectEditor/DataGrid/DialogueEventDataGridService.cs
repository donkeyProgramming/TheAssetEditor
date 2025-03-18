using System.Collections.Generic;
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
            SetDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.AudioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = DataGridHelpers.GetStatesForStateGroupColumn(_audioEditorService.AudioEditorViewModel, _audioRepository, _audioEditorService, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData()
        {
            var rowData = new Dictionary<string, string>();

            var stateGroupsWithAnyState = _audioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.AudioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = _audioRepository.GetStateGroupFromStateGroupWithQualifier(dialogueEvent.Name, DataGridHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    rowData[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            _audioEditorService.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }
    }
}
