using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.Storage;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public class DialogueEventDataService : IAudioProjectDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);

            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.DialogueEventsWithStateGroups[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnHeader = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = GetStatesForColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, columnHeader, columnWidth, DataGridColumnType.StateGroupEditableComboBox, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, string>();

            var stateGroupsWithAnyState = parameters.AudioRepository.StateGroupsWithStates
                .Where(stateGroupColumn => stateGroupColumn.Value.Contains("Any"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var columnName = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var stateGroup = parameters.AudioRepository.GetStateGroupFromStateGroupWithQualifier(parameters.DialogueEvent.Name, AudioProjectHelpers.RemoveExtraUnderscoresFromString(columnName));

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    rowData[columnName] = "Any";
            }

            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGridTag);
            DataGridHelpers.CreateContextMenu(parameters, dataGrid);

            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];

            var stateGroupsCount = parameters.AudioRepository.DialogueEventsWithStateGroups[parameters.DialogueEvent.Name].Count;
            var columnWidth = 1.0 / (1 + stateGroupsCount);

            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                var states = GetStatesForColumn(parameters, stateGroupWithQualifier.Value);
                var stateGroupColumn = DataGridHelpers.CreateColumn(parameters, stateGroupColumnHeader, columnWidth, DataGridColumnType.ReadOnlyTextBlock, states);
                dataGrid.Columns.Add(stateGroupColumn);
            }
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];

            foreach (var statePath in parameters.DialogueEvent.DecisionTree)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupColumnHeader = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                    var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                    if (node != null)
                        rowData[stateGroupColumnHeader] = node.State.Name;
                    else
                        rowData[stateGroupColumnHeader] = string.Empty;
                }

                parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Add(rowData);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var statePath = AudioProjectHelpers.CreateStatePathFromDataGridRow(parameters.AudioRepository, parameters.AudioProjectEditorRow, parameters.DialogueEvent);
            statePath.AudioSettings = parameters.AudioEditorViewModel.AudioSettingsViewModel.BuildAudioSettings();
            AudioProjectHelpers.InsertStatePathAlphabetically(parameters.DialogueEvent, statePath);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = AudioProjectHelpers.GetStatePathFromDataGridRow(parameters.AudioRepository, dataGridRow, parameters.DialogueEvent);
                if (statePath != null)
                {
                    parameters.DialogueEvent.DecisionTree.Remove(statePath);
                    parameters.AudioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid.Remove(dataGridRow);
                }
            }
        }

        private static List<string> GetStatesForColumn(AudioProjectDataServiceParameters parameters, string stateGroup)
        {
            var states = new List<string>();
            var moddedStates = GetModdedStates(parameters, stateGroup);
            var vanillaStates = parameters.AudioRepository.StateGroupsWithStates[stateGroup];

            // Display the required states in the ComboBox
            if (parameters.AudioEditorViewModel.AudioProjectEditorViewModel.ShowModdedStatesOnly && ModdedStateGroups.Contains(stateGroup))
                states.AddRange(moddedStates);
            else
            {
                if (moddedStates.Count > 0)
                    states.AddRange(moddedStates);

                states.AddRange(vanillaStates);
            }

            return states;
        }

        private static List<string> GetModdedStates(AudioProjectDataServiceParameters parameters, string stateGroup)
        {
            var moddedStates = new List<string>();

            if (parameters.AudioProjectService.StateGroupsWithModdedStatesRepository.TryGetValue(stateGroup, out var audioProjectModdedStates))
            {
                moddedStates.Add("Any"); // The Any State is in AudioRepository.StateGroupsWithStates but not in moddedStates so just add it in manually
                moddedStates.AddRange(audioProjectModdedStates);
                return moddedStates;
            }

            return moddedStates;
        }
    }
}
