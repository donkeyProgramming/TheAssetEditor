using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.DataGrids
{
    internal static class DataGridHelpers
    {
        public static void ClearDataGridColumns(DataGrid dataGrid)
        {
            dataGrid.Columns.Clear();
        }

        public static void ClearDataGridContextMenu(DataGrid dataGrid)
        {
            if (dataGrid.ContextMenu != null)
                dataGrid.ContextMenu.Items.Clear();
        }

        public static T FindVisualChild<T>(DependencyObject parent, string identifier) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && child is FrameworkElement element)
                {
                    // Check both Name and Tag because DataGrids use Tag as Name can't be set via a binding for some reason...
                    if (element.Name == identifier || element.Tag?.ToString() == identifier)
                        return typedChild;
                }

                var foundChild = FindVisualChild<T>(child, identifier);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }

        public static DataGrid GetDataGridByTag(string dataGridTag)
        {
            var mainWindow = Application.Current.MainWindow;
            return FindVisualChild<DataGrid>(mainWindow, dataGridTag);
        }

        public static void InsertRowAlphabetically(
            DataTable viewerTable,
            DataRow editorRow)
        {
            var newValue = editorRow[0]?.ToString() ?? string.Empty;

            var insertIndex = 0;

            for (var i = 0; i < viewerTable.Rows.Count; i++)
            {
                var currentValue = viewerTable.Rows[i][0]?.ToString() ?? string.Empty;
                var comparison = string.Compare(newValue, currentValue, StringComparison.Ordinal);

                if (comparison < 0)
                {
                    insertIndex = i;
                    break;
                }

                insertIndex = i + 1;
            }

            var newRow = viewerTable.NewRow();
            newRow.ItemArray = editorRow.ItemArray;
            viewerTable.Rows.InsertAt(newRow, insertIndex);
        }


        public static List<string> GetStatesForStateGroupColumn(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioEditorService audioEditorService, string stateGroup)
        {
            var states = new List<string>();
            var moddedStates = GetModdedStates(audioEditorService, stateGroup);
            var vanillaStates = audioRepository.StatesLookupByStateGroup[stateGroup];

            // Display the required states in the ComboBox
            if (audioEditorViewModel.AudioProjectEditorViewModel.ShowModdedStatesOnly && StateGroups.ModdedStateGroups.Contains(stateGroup))
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

        private static List<string> GetModdedStates(IAudioEditorService audioEditorService, string stateGroup)
        {
            var moddedStates = new List<string>();

            if (audioEditorService.ModdedStatesByStateGroupLookup.TryGetValue(stateGroup, out var audioProjectModdedStates))
            {
                moddedStates.AddRange(audioProjectModdedStates);
                return moddedStates;
            }

            return moddedStates;
        }

        public static string AddExtraUnderscoresToString(string wtfWpf)
        {
            return wtfWpf.Replace("_", "__");
        }

        public static string RemoveExtraUnderscoresFromString(string wtfWpf)
        {
            return wtfWpf.Replace("__", "_");
        }
    }
}
