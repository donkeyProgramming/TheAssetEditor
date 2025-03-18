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
            SetDataGridData();
        }

        public void ConfigureDataGrid()
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.AudioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData()
        {
            var dataGridRow = new Dictionary<string, string> { };
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, _audioEditorService.AudioEditorViewModel.GetSelectedAudioProjectNodeName());
            dataGridRow[DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = string.Empty;
            _audioEditorService.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }
    }
}
