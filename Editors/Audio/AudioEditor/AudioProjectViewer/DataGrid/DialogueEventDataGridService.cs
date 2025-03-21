using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class DialogueEventDataGridService : IAudioProjectViewerDataGridService
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
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            DataGridConfiguration.CreateContextMenu(_audioEditorService.AudioEditorViewModel, dataGrid);

            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);

            var stateGroupsCount = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, stateGroupColumnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetDataGridData()
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];

            foreach (var statePath in dialogueEvent.StatePaths)
                ProcessStatePathData(stateGroupsWithQualifiers, statePath);
        }

        private void ProcessStatePathData(Dictionary<string, string> stateGroupsWithQualifiers, StatePath statePath)
        {
            var rowData = new Dictionary<string, string>();

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                if (node != null)
                    rowData[stateGroupColumnHeader] = node.State.Name;
                else
                    rowData[stateGroupColumnHeader] = string.Empty;
            }

            _audioEditorService.GetViewerDataGrid().Add(rowData);
        }
    }
}
