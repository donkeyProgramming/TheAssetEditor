using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.DataServices;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public class ActionEventDataGridService : IAudioProjectViewerDataGridService
    {
        private readonly IAudioProjectService _audioProjectService;

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
        }

        public void ConfigureDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetDataGridData(DataServiceParameters parameters)
        {
            foreach (var actionEvent in parameters.SoundBank.ActionEvents)
            {
                var rowData = new Dictionary<string, string>
                {
                    { "Event", actionEvent.Name }
                };
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
            }
        }
    }
}
