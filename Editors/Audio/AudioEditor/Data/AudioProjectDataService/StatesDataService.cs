using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public class StatesDataService : IAudioProjectDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, AudioProjectHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow[AudioProjectHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = string.Empty;
            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(dataGridRow);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, AudioProjectHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
        {
            foreach (var state in parameters.StateGroup.States)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow[AudioProjectHelpers.AddExtraUnderscoresToString(parameters.StateGroup.Name)] = state.Name;
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(dataGridRow);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var rowData = parameters.AudioProjectEditorRow.First();
            var state = new State();
            state.Name = rowData.Value.ToString();
            AudioProjectHelpers.InsertStateAlphabetically(parameters.StateGroup, state);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = AudioProjectHelpers.GetStateMatchingWithDataGridRow(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid, dataGridRow, parameters.StateGroup);
                parameters.StateGroup.States.Remove(state);
                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
            }
        }
    }
}
