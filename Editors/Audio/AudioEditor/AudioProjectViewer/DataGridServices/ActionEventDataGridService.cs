using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;

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
            ConfigureDataGrid(audioEditorViewModel);
            SetDataGridData(audioEditorViewModel);
        }

        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var rowData = new Dictionary<string, string>
                {
                    { "Event", actionEvent.Name }
                };
                audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
            }
        }
    }
}
