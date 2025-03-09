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

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var stateGroupColumn = DataGridConfiguration.CreateColumn(audioEditorViewModel, DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            foreach (var state in stateGroup.States)
            {
                var dataGridRow = new Dictionary<string, string>();
                dataGridRow[DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = state.Name;
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(dataGridRow);
            }
        }
    }
}
