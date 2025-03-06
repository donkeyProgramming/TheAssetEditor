using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridService
{
    public class StateGroupDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;
        private readonly IAudioRepository _audioRepository;

        private readonly ILogger _logger = Logging.Create<StateGroupDataGridService>();

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
                AudioProjectService = _audioProjectService,
                AudioRepository = _audioRepository,
                StateGroup = stateGroup
            };

            ConfigureDataGrid(parameters);
            SetDataGridData(parameters);

            _logger.Here().Information($"Loaded State Group {stateGroup.Name} in Audio Project Editor");
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            var dataGridRow = new Dictionary<string, string> { };
            dataGridRow[DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = string.Empty;
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }
    }
}
