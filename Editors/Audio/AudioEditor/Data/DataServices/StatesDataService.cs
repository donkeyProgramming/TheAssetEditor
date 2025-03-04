using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class StatesDataService : IDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectEditorDataGridData(DataServiceParameters parameters)
        {
            var dataGridRow = new Dictionary<string, string> { };
            dataGridRow[DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = string.Empty;
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }

        public void ConfigureAudioProjectViewerDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectViewerDataGridData(DataServiceParameters parameters)
        {
            foreach (var state in parameters.StateGroup.States)
            {
                var dataGridRow = new Dictionary<string, string>();
                dataGridRow[DataHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = state.Name;
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(dataGridRow);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(DataServiceParameters parameters)
        {
            var state = DataHelpers.CreateStateFromDataGridRow(parameters.AudioProjectEditorRow);
            DataHelpers.InsertStateAlphabetically(parameters.StateGroup, state);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(DataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = DataHelpers.GetStateFromDataGridRow(dataGridRow, parameters.StateGroup);
                parameters.StateGroup.States.Remove(state);
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
