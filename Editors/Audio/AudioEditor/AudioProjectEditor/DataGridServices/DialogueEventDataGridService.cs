using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices
{
    public class DialogueEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public DialogueEventDataGridService(IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
        }

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = DataHelpers.GetStatesForStateGroupColumn(audioEditorViewModel, _audioRepository, _audioProjectService, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var rowData = new Dictionary<string, string>();

            var stateGroupsWithAnyState = _audioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = _audioRepository.GetStateGroupFromStateGroupWithQualifier(dialogueEvent.Name, DataHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    rowData[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }
    }
}
