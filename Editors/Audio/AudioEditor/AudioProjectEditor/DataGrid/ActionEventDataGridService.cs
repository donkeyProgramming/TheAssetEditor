using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.GameSettings.Warhammer3;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public class ActionEventDataGridService : IAudioProjectEditorDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;

        public ActionEventDataGridService(IAudioEditorService audioEditorService)
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
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            if (soundBank.Name == SoundBanks.MoviesDisplayString)
            {
                var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
                dataGrid.Columns.Add(eventColumn);

                var fileSelectColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, string.Empty, 25, DataGridColumnType.FileSelectButton, useAbsoluteWidth: true);
                dataGrid.Columns.Add(fileSelectColumn);
            }
            else
            {
                var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, "Event", columnWidth, DataGridColumnType.EditableTextBox);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void SetDataGridData()
        {
            var rowData = new Dictionary<string, string>
            {
                { "Event", string.Empty }
            };
            _audioEditorService.GetEditorDataGrid().Add(rowData);
        }
    }
}
