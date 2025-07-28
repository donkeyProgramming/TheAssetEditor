using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Presentation.Table
{
    internal static class TableHelpers
    {
        public static string DuplicateUnderscores(string wtfWpf)
        {
            return wtfWpf.Replace("_", "__");
        }

        public static string DeduplicateUnderscores(string wtfWpf)
        {
            return wtfWpf.Replace("__", "_");
        }

        public static List<string> GetStatesForStateGroupColumn(IAudioEditorService audioEditorService, IAudioRepository audioRepository, string stateGroup)
        {
            var states = new List<string>();
            var moddedStates = audioEditorService.AudioProject.StateGroups
                .SelectMany(stateGroup => stateGroup.States)
                .Select(state => state.Name);
            var vanillaStates = audioRepository.StatesLookupByStateGroup[stateGroup];

            // Display the required states in the ComboBox
            if (audioEditorService.ShowModdedStatesOnly && StateGroups.ModdedStateGroups.Contains(stateGroup))
            {
                states.Add("Any"); // We still want the "Any" state to show so add it in manually
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

        public static string GetStateGroupFromStateGroupWithQualifier(IAudioRepository audioRepository, string dialogueEvent, string stateGroupNameWithQualifier)
        {
            if (audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent.TryGetValue(dialogueEvent, out var stateGroupDictionary))
                if (stateGroupDictionary.TryGetValue(stateGroupNameWithQualifier, out var stateGroup))
                    return stateGroup;

            return null;
        }

        public static string GetValueFromRow(DataRow row, string columnName)
        {
            return row[columnName].ToString();
        }

        public static string GetActionEventNameFromRow(DataRow row)
        {
            return GetValueFromRow(row, TableInfo.EventColumnName);
        }

        public static string GetStatePathNameFromRow(DataRow row, IAudioRepository audioRepository, string dialogueEventName)
        {
            var statePathNodes = new List<StatePathNode>();
            foreach (DataColumn column in row.Table.Columns)
            {
                // CA sometimes add new State Groups into a Dialogue Event
                // When that Dialogue Event already contains State Paths the new State Group's value in the path is set to empty
                // So we skip any State Groups with empty values so we can actually get the State Path that doesn't contain the new empty data
                var value = row[column];
                if (value == DBNull.Value)
                    continue;

                var stateName = value.ToString();
                if (string.IsNullOrEmpty(stateName))
                    continue;

                var stateGroupNameWithQualifier = DeduplicateUnderscores(column.ColumnName);
                var stateGroupName = GetStateGroupFromStateGroupWithQualifier(audioRepository, dialogueEventName, stateGroupNameWithQualifier);

                var statePathNode = new StatePathNode
                {
                    StateGroup = new StateGroup { Name = stateGroupName },
                    State = new State { Name = stateName }
                };

                statePathNodes.Add(statePathNode);


            }
            var statePathName = StatePath.BuildName(statePathNodes);
            return statePathName;
        }

        public static string GetStateNameFromRow(DataRow row)
        {
            return GetValueFromRow(row, TableInfo.StateColumnName);
        }

        public static void InsertRowAlphabetically(DataTable table, DataRow row)
        {
            var newValue = row[0]?.ToString() ?? string.Empty;

            var insertIndex = 0;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var currentValue = table.Rows[i][0]?.ToString() ?? string.Empty;
                var comparison = string.Compare(newValue, currentValue, StringComparison.Ordinal);

                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }

                insertIndex = i + 1;
            }

            var newRow = table.NewRow();
            newRow.ItemArray = row.ItemArray;
            table.Rows.InsertAt(newRow, insertIndex);
        }
    }
}
