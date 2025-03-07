using System.Collections.Generic;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.GameSettings.Warhammer3;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices
{
    public class ActionEventDataGridService : IAudioProjectEditorDataGridService
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
            var dataGrid = DataGridHelpers.InitialiseDataGrid(audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var soundBank = DataHelpers.GetSoundBankFromName(_audioProjectService, audioEditorViewModel.GetSelectedAudioProjectNodeName());
            if (soundBank.Name == SoundBanks.MoviesDisplayString)
            {
                var eventColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
                dataGrid.Columns.Add(eventColumn);

                var fileSelectColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, string.Empty, 25, DataGridColumnType.FileSelectButton, useAbsoluteWidth: true);
                dataGrid.Columns.Add(fileSelectColumn);
            }
            else
            {
                var eventColumn = DataGridHelpers.CreateColumn(audioEditorViewModel, "Event", columnWidth, DataGridColumnType.EditableTextBox);
                dataGrid.Columns.Add(eventColumn);
            }
        }

        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel)
        {
            var rowData = new Dictionary<string, string>
            {
                { "Event", string.Empty }
            };
            audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }
    }
}
