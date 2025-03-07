using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
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
            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            DataGridHelpers.CreateContextMenu(audioEditorViewModel, dataGrid);

            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());

            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = DataHelpers.GetStatesForStateGroupColumn(audioEditorViewModel, _audioRepository, _audioProjectService, stateGroupWithQualifier.Value);

                var stateGroupColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, stateGroupColumnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var dialogueEvent = DataHelpers.GetDialogueEventFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];

            foreach (var statePath in dialogueEvent.StatePaths)
                ProcessStatePathData(audioEditorViewModel, stateGroupsWithQualifiers, statePath);
        }

        private static void ProcessStatePathData(AudioEditorViewModel audioEditorViewModel, Dictionary<string, string> stateGroupsWithQualifiers, StatePath statePath)
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

            audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
        }
    }
}
