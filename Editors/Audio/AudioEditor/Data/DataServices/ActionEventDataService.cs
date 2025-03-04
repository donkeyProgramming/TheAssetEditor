using System.Collections.Generic;
using System.Linq;
using Editors.Audio.GameSettings.Warhammer3;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class ActionEventDataService : IDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(DataServiceParameters parameters)
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

        public void SetAudioProjectEditorDataGridData(DataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>
            {
                { "Event", string.Empty }
            };
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetAudioProjectViewerDataGridData(DataServiceParameters parameters)
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

        public void AddAudioProjectEditorDataGridDataToAudioProject(DataServiceParameters parameters)
        {
            var actionEvent = DataHelpers.CreateActionEvent(parameters.AudioEditorViewModel.AudioSettingsViewModel, parameters.AudioProjectEditorRow);
            var soundBank = parameters.SoundBank;
            DataHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(DataServiceParameters parameters)
        {
            var soundBank = DataHelpers.GetSoundBankFromName(parameters.AudioProjectService, parameters.AudioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);

            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = DataHelpers.GetActionEventFromDataGridRow(dataGridRow, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
