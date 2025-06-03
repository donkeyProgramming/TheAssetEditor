using System;
using System.Data;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class DataTableHelpers
    {
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
