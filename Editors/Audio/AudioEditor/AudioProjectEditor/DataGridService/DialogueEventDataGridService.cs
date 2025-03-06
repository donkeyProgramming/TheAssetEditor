using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridService
{
    public class DialogueEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        private readonly ILogger _logger = Logging.Create<DialogueEventDataGridService>();

        public DialogueEventDataGridService(IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
        }

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            if (_audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = _audioProjectService,
                AudioRepository = _audioRepository,
                DialogueEvent = dialogueEvent
            };

            ConfigureDataGrid(parameters);
            SetDataGridData(parameters);

            _logger.Here().Information($"Loaded Dialogue Event: {dialogueEvent.Name} in in Audio Project Editor");
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.StateGroupsLookupByDialogueEvent[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = DataHelpers.GetStatesForStateGroupColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>();

            var stateGroupsWithAnyState = parameters.AudioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = parameters.AudioRepository.GetStateGroupFromStateGroupWithQualifier(parameters.DialogueEvent.Name, DataHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    rowData[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }
    }
}
