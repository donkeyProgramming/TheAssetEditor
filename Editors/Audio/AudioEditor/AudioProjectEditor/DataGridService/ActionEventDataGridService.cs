using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;
using Editors.Audio.GameSettings.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridService
{
    public class ActionEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;

        private readonly ILogger _logger = Logging.Create<ActionEventDataGridService>();

        public ActionEventDataGridService(IAudioProjectService audioProjectService)
        {
            _audioProjectService = audioProjectService;
        }

        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            var parameters = new DataServiceParameters
            {
                AudioEditorViewModel = audioEditorViewModel,
                AudioProjectService = _audioProjectService,
                SoundBank = soundBank
            };

            ConfigureDataGrid(parameters);
            SetDataGridData(parameters);

            _logger.Here().Information($"Loaded Action Event {soundBank.Name} in Audio Project Editor");
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            if (parameters.SoundBank.Name == SoundBanks.MoviesDisplayString)
            {
                var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
                dataGrid.Columns.Add(eventColumn);

                var fileSelectColumn = DataGridHelpers.CreateColumn(parameters, string.Empty, 25, DataGridColumnType.FileSelectButton, useAbsoluteWidth: true);
                dataGrid.Columns.Add(fileSelectColumn);
            }
            else
            {
                var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.EditableTextBox);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>
            {
                { "Event", string.Empty }
            };
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }
    }
}
