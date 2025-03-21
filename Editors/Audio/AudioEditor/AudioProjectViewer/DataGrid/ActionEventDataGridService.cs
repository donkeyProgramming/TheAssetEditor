using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.DataGrids;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGrid
{
    public class ActionEventDataGridService : IAudioProjectViewerDataGridService
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
            var dataGrid = DataGridConfiguration.InitialiseDataGrid(_audioEditorService.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridConfiguration.CreateColumn(_audioEditorService.AudioEditorViewModel, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetDataGridData()
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService, _audioEditorService.GetSelectedExplorerNode().Name);
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var rowData = new Dictionary<string, string>
                {
                    { "Event", actionEvent.Name }
                };
                _audioEditorService.GetViewerDataGrid().Add(rowData);
            }
        }
    }
}
