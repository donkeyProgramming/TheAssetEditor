using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public class DialogueEventDataGridService : IAudioProjectViewerDataGridService
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
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = _audioProjectService,
                AudioRepository = _audioRepository,
                DialogueEvent = dialogueEvent
            };

            ConfigureDataGrid(parameters);
            SetDataGridData(parameters);
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            DataGridHelpers.CreateContextMenu(parameters, dataGrid);

            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.StateGroupsLookupByDialogueEvent[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = DataHelpers.GetStatesForStateGroupColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, stateGroupColumnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            foreach (var statePath in parameters.DialogueEvent.StatePaths)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupColumnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                    var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                    if (node != null)
                        rowData[stateGroupColumnHeader] = node.State.Name;
                    else
                        rowData[stateGroupColumnHeader] = string.Empty;
                }

                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
            }
        }
    }
}
