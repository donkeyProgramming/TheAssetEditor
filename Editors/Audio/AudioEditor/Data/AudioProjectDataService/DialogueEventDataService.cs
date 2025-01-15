using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioSettingsEditor;
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

            var audioFilesColumn = DataGridHelpers.CreateColumn(parameters, "Audio Files", columnWidth, DataGridColumnType.AudioFilesEditableTextBox);
            dataGrid.Columns.Add(audioFilesColumn);
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, object>
            {
                { "AudioFiles", new List<string>() },
                { "AudioFilesDisplay", string.Empty },
                { "AudioSettings", new AudioSettings() }
            };

            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupColumnHeader = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                rowData[stateGroupColumnHeader] = string.Empty;
            }

            parameters.AudioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorFullDataGridTag);
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

            var audioFilesColumn = DataGridHelpers.CreateColumn(parameters, "Audio Files", columnWidth, DataGridColumnType.AudioFilesReadOnlyTextBlock);
            dataGrid.Columns.Add(audioFilesColumn);
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];

            foreach (var statePath in parameters.DialogueEvent.DecisionTree)
            {
                var rowData = new Dictionary<string, object>
                {
                    { "AudioFiles", statePath.AudioFiles },
                    { "AudioFilesDisplay", statePath.AudioFilesDisplay },
                    { "AudioSettings", statePath.AudioSettings }
                };

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupColumnHeader = AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier.Key);
                    var node = statePath.Nodes.FirstOrDefault(node => node.StateGroup.Name == stateGroupWithQualifier.Value);
                    if (node != null)
                        rowData[stateGroupColumnHeader] = node.State.Name;
                    else
                        rowData[stateGroupColumnHeader] = string.Empty;
                }

                parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Add(rowData);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var statePath = new StatePath();

            var stateGroupsWithQualifiers = parameters.AudioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[parameters.DialogueEvent.Name];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers.Keys)
            {
                if (parameters.AudioProjectEditorRow.TryGetValue(AudioProjectHelpers.AddExtraUnderscoresToString(stateGroupWithQualifier), out var cellValue))
                {
                    var statePathNode = new StatePathNode
                    {
                        StateGroup = new StateGroup(),
                        State = new State()
                    };
                    statePathNode.StateGroup.Name = stateGroupsWithQualifiers[stateGroupWithQualifier];
                    statePathNode.State.Name = cellValue.ToString();
                    statePath.Nodes.Add(statePathNode);
                }
            }

            if (parameters.AudioProjectEditorRow.TryGetValue("AudioFiles", out var audioFiles))
            {
                var filePaths = audioFiles as List<string>;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                statePath.AudioFiles = filePaths;
                statePath.AudioFilesDisplay = fileNamesString;
            }

            if (parameters.AudioProjectEditorRow.TryGetValue("AudioSettings", out var audioSettings))
                statePath.AudioSettings = AudioSettingsEditorViewModel.BuildAudioSettings(parameters.AudioEditorViewModel.AudioSettingsViewModel);

            AudioProjectHelpers.InsertStatePathAlphabetically(parameters.DialogueEvent, statePath);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var statePath = AudioProjectHelpers.GetStatePathFromDialogueEvent(parameters.AudioRepository, dataGridRow, parameters.DialogueEvent);
                var matchingStatePath = AudioProjectHelpers.GetMatchingDecisionNode(statePath, parameters.DialogueEvent);
                if (matchingStatePath != null)
                {
                    parameters.DialogueEvent.DecisionTree.Remove(matchingStatePath);
                    parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Remove(dataGridRow);
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
