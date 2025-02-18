using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.AudioProjectData.AudioProjectDataService
{
    public class ActionEventDataService : IAudioProjectDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>
            {
                { "Event", string.Empty }
            };
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
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

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var actionEvent = AudioProjectHelpers.CreateActionEvent(parameters.AudioEditorViewModel.AudioSettingsViewModel, parameters.AudioProjectEditorRow);
            var soundBank = parameters.SoundBank;
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(parameters.AudioProjectService, parameters.AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = AudioProjectHelpers.GetActionEventFromDataGridRow(dataGridRow, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
