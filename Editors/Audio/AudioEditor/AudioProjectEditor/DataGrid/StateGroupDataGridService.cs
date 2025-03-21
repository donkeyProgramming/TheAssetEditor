using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class StateGroupDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public StateGroupDataGridService(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid()
        {
            ConfigureDataGrid();
            InitialiseDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void InitialiseDataGridData()
        {
            var dataGridRow = new Dictionary<string, string> { };
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            dataGridRow[DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = string.Empty;
            _audioEditorService.GetEditorDataGrid().Add(dataGridRow);
        }

        public void SetDataGridData()
        {
            _audioEditorService.GetEditorDataGrid().Add(_audioEditorService.GetSelectedViewerRows()[0]);
        }
    }
}
