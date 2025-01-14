using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.DataGrids.AudioProjectDataService;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class StatesDataService : IAudioProjectDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters) 
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow[AddExtraUnderscoresToString(parameters.StateGroup.Name)] = string.Empty;
            parameters.AudioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(dataGridRow);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorFullDataGridTag);
            var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, AddExtraUnderscoresToString(parameters.StateGroup.Name), 1.0, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
        {
            foreach (var state in parameters.StateGroup.States)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow[AddExtraUnderscoresToString(parameters.StateGroup.Name)] = state.Name;
                parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Add(dataGridRow);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var rowData = parameters.AudioProjectEditorRow.First();
            var state = new State();
            state.Name = rowData.Value.ToString();
            InsertStateAlphabetically(parameters.StateGroup, state);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var state = GetMatchingState(parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid, dataGridRow, parameters.StateGroup);
                parameters.StateGroup.States.Remove(state);
                parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Remove(dataGridRow);
            }
        }
    }
}
