using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.DB
{
    public class DbTableDefinition
    {
        public string TableName { get; set; }
        public int Version { get; set; }
        public List<DbColumnDefinition> ColumnDefinitions { get; set; } = new List<DbColumnDefinition>();

        public string DisplayName { get => TableName + "_v" + Version; }
    }
}
