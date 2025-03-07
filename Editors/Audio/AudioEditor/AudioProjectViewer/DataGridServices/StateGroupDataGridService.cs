using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public class StateGroupDataGridService : IAudioProjectViewerDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        public StateGroupDataGridService(IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            _audioProjectService = audioProjectService;
            _audioRepository = audioRepository;
        }

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var stateGroup = DataHelpers.GetStateGroupFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioRepository = _audioRepository,
                AudioProjectService = _audioProjectService,
                StateGroup = stateGroup
            };

            ConfigureDataGrid(parameters);
            SetDataGridData(parameters);
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            foreach (var state in parameters.StateGroup.States)
            {
                var dataGridRow = new Dictionary<string, string>();
                dataGridRow[DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = state.Name;
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(dataGridRow);
            }
        }
    }
}
