using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class StateGroupDataGridService : IAudioProjectViewerDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public StateGroupDataGridService(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid()
        {
            ConfigureDataGrid();
            SetDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            foreach (var state in stateGroup.States)
            {
                var dataGridRow = new Dictionary<string, string>();
                dataGridRow[DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = state.Name;
                _audioEditorService.GetViewerDataGrid().Add(dataGridRow);
            }
        }

        public void InsertDataGridRow()
        {
            DataGridHelpers.InsertRowAlphabetically(_audioEditorService.GetViewerDataGrid(), _audioEditorService.GetEditorDataGrid()[0]);
        }
    }
}
