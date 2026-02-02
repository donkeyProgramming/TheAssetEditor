using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.Ui.Common;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Table
{
    public static class TableHelpers
    {
        public static List<string> GetStatesForStateGroupColumn(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, string stateGroup)
        {
            var states = new List<string>();
            var moddedStates = audioEditorStateService.AudioProject.StateGroups
                .Where(moddedStateGroup => moddedStateGroup.Name == stateGroup)
                .SelectMany(moddedStateGroup => moddedStateGroup.States)
                .Select(moddedState => moddedState.Name);
            var vanillaStates = audioRepository.StatesByStateGroup[stateGroup];

            // Display the required states in the ComboBox
            if (audioEditorStateService.ShowModdedStatesOnly && Wh3StateGroupInformation.ModdableStateGroups.Contains(stateGroup))
            {
                // We still want the "Any" state to show so add it in manually
                states.Add("Any"); 
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

            return states.Distinct().ToList();
        }

        public static string GetStateGroupFromStateGroupWithQualifier(IAudioRepository audioRepository, string dialogueEvent, string stateGroupNameWithQualifier)
        {
            if (audioRepository.QualifiedStateGroupByStateGroupByDialogueEvent.TryGetValue(dialogueEvent, out var stateGroupDictionary))
                if (stateGroupDictionary.TryGetValue(stateGroupNameWithQualifier, out var stateGroup))
                    return stateGroup;

            return null;
        }

        public static string GetValueFromRow(DataRow row, string columnName) => row[columnName].ToString();

        public static string GetActionEventNameFromRow(DataRow row) => GetValueFromRow(row, TableInformation.ActionEventColumnName);

        public static string GetStatePathNameFromRow(DataRow row, IAudioRepository audioRepository, string dialogueEventName)
        {
            var statePathNodes = new List<StatePath.Node>();
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

                var stateGroupNameWithQualifier = WpfHelpers.DeduplicateUnderscores(column.ColumnName);
                var stateGroupName = GetStateGroupFromStateGroupWithQualifier(audioRepository, dialogueEventName, stateGroupNameWithQualifier);

                var statePathNode = new StatePath.Node
                {
                    StateGroup = new StateGroup { Name = stateGroupName },
                    State = new State { Name = stateName }
                };

                statePathNodes.Add(statePathNode);
            }
            var statePathName = StatePath.BuildName(statePathNodes);
            return statePathName;
        }

        public static string GetStateNameFromRow(DataRow row) => GetValueFromRow(row, TableInformation.StateColumnName);

        public static void InsertRowAlphabeticallyByStatePathName(DataTable table, DataRow sourceRow, IAudioRepository audioRepository, string dialogueEventName)
        {
            var newKey = GetStatePathNameFromRow(sourceRow, audioRepository, dialogueEventName);
            var comparer = StringComparer.OrdinalIgnoreCase;

            var insertAt = table.Rows.Count;
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var existingKey = GetStatePathNameFromRow(table.Rows[i], audioRepository, dialogueEventName);

                var comparisonResult = comparer.Compare(newKey, existingKey);

                if (comparisonResult == 0)
                    return;

                if (comparisonResult < 0)
                {
                    insertAt = i;
                    break;
                }
            }

            var newRow = table.NewRow();
            newRow.ItemArray = (object[])sourceRow.ItemArray.Clone();
            table.Rows.InsertAt(newRow, insertAt);
        }

        public static void InsertRowAlphabeticallyByStateName(DataTable table, DataRow sourceRow)
        {
            var newKey = GetStateNameFromRow(sourceRow);
            var comparer = StringComparer.OrdinalIgnoreCase;
            var insertAt = table.Rows.Count;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var existingKey = GetStateNameFromRow(table.Rows[i]);
                var comparisonResult  = comparer.Compare(newKey, existingKey);

                if (comparisonResult  == 0) return;
                if (comparisonResult  < 0) { insertAt = i; break; }
            }

            var newRow = table.NewRow();
            newRow.ItemArray = (object[])sourceRow.ItemArray.Clone();
            table.Rows.InsertAt(newRow, insertAt);
        }

        public static void InsertRowAlphabeticallyByActionEventName(DataTable table, DataRow sourceRow)
        {
            var newKey = GetActionEventNameFromRow(sourceRow);
            var comparer = StringComparer.OrdinalIgnoreCase;
            var insertAt = table.Rows.Count;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var existingKey = GetActionEventNameFromRow(table.Rows[i]);
                var comparisonResult  = comparer.Compare(newKey, existingKey);

                if (comparisonResult  == 0) return;
                if (comparisonResult  < 0) { insertAt = i; break; }
            }

            var newRow = table.NewRow();
            newRow.ItemArray = (object[])sourceRow.ItemArray.Clone();
            table.Rows.InsertAt(newRow, insertAt);
        }

        public static DataRow CreateRow(DataTable table, string actionEventName)
        {
            var pseudoTable = table.Clone();
            var row = pseudoTable.NewRow();
            row[TableInformation.ActionEventColumnName] = actionEventName;
            return row;
        }

        public static string RemoveActionEventPrefix(string actionEventName)
        {
            if (actionEventName.StartsWith("Play_", StringComparison.Ordinal))
                return actionEventName.Substring("Play_".Length);
            if (actionEventName.StartsWith("Pause_", StringComparison.Ordinal))
                return actionEventName.Substring("Pause_".Length);
            if (actionEventName.StartsWith("Resume_", StringComparison.Ordinal))
                return actionEventName.Substring("Resume_".Length);
            if (actionEventName.StartsWith("Stop_", StringComparison.Ordinal))
                return actionEventName.Substring("Stop_".Length);
            return actionEventName;
        }

        public static bool IsPauseResumeStopActionEvent(string actionEventName)
        {
            return actionEventName.StartsWith("Pause_", StringComparison.Ordinal)
                || actionEventName.StartsWith("Resume_", StringComparison.Ordinal)
                || actionEventName.StartsWith("Stop_", StringComparison.Ordinal);
        }
    }
}
