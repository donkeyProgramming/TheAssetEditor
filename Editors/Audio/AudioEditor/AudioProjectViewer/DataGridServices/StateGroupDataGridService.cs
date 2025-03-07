using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public class StateGroupDataGridService : IAudioProjectViewerDataGridService
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
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            var stateGroupColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, DataHelpers.AddExtraUnderscoresToString(stateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            foreach (var state in stateGroup.States)
            {
                var dataGridRow = new Dictionary<string, string>();
                dataGridRow[DataHelpers.AddExtraUnderscoresToString(stateGroup.Name)] = state.Name;
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(dataGridRow);
            }
        }
    }
}
