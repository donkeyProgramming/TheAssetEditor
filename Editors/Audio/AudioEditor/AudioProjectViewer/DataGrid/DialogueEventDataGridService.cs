using System.Collections.Generic;
using System.Data;
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

            var table = _audioEditorService.GetViewerDataGrid();

            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);

                if (!table.Columns.Contains(columnHeader))
                    table.Columns.Add(new DataColumn(columnHeader, typeof(string)));

                var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, columnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock);
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
            var table = _audioEditorService.GetViewerDataGrid();
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

            table.Rows.Add(row);
        }

        public void InsertDataGridRow()
        {
            DataGridHelpers.InsertRowAlphabetically(_audioEditorService.GetViewerDataGrid(), _audioEditorService.GetEditorDataGrid().Rows[0]);
        }
    }
}
