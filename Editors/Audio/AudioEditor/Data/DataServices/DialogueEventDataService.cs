using System.Collections.Generic;
using System.Linq;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class DialogueEventDataService : IDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.StateGroupsLookupByDialogueEvent[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = GetStatesForColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetAudioProjectEditorDataGridData(DataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>();

            var stateGroupsWithAnyState = parameters.AudioRepository.StatesLookupByStateGroup
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = parameters.AudioRepository.GetStateGroupFromStateGroupWithQualifier(parameters.DialogueEvent.Name, DataHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    rowData[columnName] = "Any"; // Set the cell value to Any as the default value
            }

            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(DataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            DataGridHelpers.CreateContextMenu(parameters, dataGrid);

            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.StateGroupsLookupByDialogueEvent[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = GetStatesForColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, stateGroupColumnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetAudioProjectViewerDataGridData(DataServiceParameters parameters)
        {
            var stateGroupsWithQualifiers = parameters.AudioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[parameters.DialogueEvent.Name];

            foreach (var statePath in parameters.DialogueEvent.StatePaths)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupColumnHeader = DataHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                    var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                    if (node != null)
                        rowData[stateGroupColumnHeader] = node.State.Name;
                    else
                        rowData[stateGroupColumnHeader] = string.Empty;
                }

                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(DataServiceParameters parameters)
        {
            var statePath = DataHelpers.CreateStatePath(parameters.AudioRepository, parameters.AudioEditorViewModel.AudioSettingsViewModel, parameters.AudioProjectEditorRow, parameters.DialogueEvent);
            DataHelpers.InsertStatePathAlphabetically(parameters.DialogueEvent, statePath);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(DataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = DataHelpers.GetStatePathFromDataGridRow(parameters.AudioRepository, dataGridRow, parameters.DialogueEvent);
                if (statePath != null)
                {
                    parameters.DialogueEvent.StatePaths.Remove(statePath);
                    parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
                }
            }
        }

        private static List<string> GetStatesForColumn(DataServiceParameters parameters, string stateGroup)
        {
            var states = new List<string>();
            var moddedStates = GetModdedStates(parameters, stateGroup);
            var vanillaStates = parameters.AudioRepository.StatesLookupByStateGroup[stateGroup];

            // Display the required states in the ComboBox
            if (parameters.AudioEditorViewModel.AudioProjectEditorViewModel.ShowModdedStatesOnly && ModdedStateGroups.Contains(stateGroup))
            {
                states.Add("Any"); // We still want the "Any" state to show so add it in manually.
                states.AddRange(moddedStates);
            }
            else
            {
                states = moddedStates
                    .Concat(vanillaStates)
                    .OrderByDescending(state => state == "Any") // "Any" becomes true and sorts first
                    .ThenBy(state => state) // Then sort the rest alphabetically
                    .ToList();
            }

            return states;
        }

        private static List<string> GetModdedStates(DataServiceParameters parameters, string stateGroup)
        {
            var moddedStates = new List<string>();

            if (parameters.AudioProjectService.StateGroupsWithModdedStatesRepository.TryGetValue(stateGroup, out var audioProjectModdedStates))
            {
                moddedStates.AddRange(audioProjectModdedStates);
                return moddedStates;
            }

            return moddedStates;
        }
    }
}
