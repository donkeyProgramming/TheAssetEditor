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

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridConfiguration.CreateColumn(audioEditorViewModel, DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGridRow = new Dictionary<string, string> { };
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            dataGridRow[DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = string.Empty;
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }
    }
}
