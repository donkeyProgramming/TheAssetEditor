using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices
{
    public class StateGroupDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;

        public StateGroupDataGridService(IAudioProjectService audioProjectService)
        {
            _audioProjectService = audioProjectService;
        }

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, DataHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGridRow = new Dictionary<string, string> { };
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            dataGridRow[DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = string.Empty;
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }
    }
}
