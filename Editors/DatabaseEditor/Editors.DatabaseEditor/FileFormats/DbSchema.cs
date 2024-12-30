using System.Diagnostics;
using Shared.Core.Settings;

namespace Editors.DatabaseEditor.FileFormats
{
    public class DbSchema
    {
        public required GameTypeEnum Game { get; set; }
        public List<DBTableSchema> TableSchemas { get; set; } = [];
    }

    [DebuggerDisplay("{Name} : {Version}")]
    public class DBTableSchema
    {
        public required int Version { get; set; }
        public required string Name { get; set; }
        public List<DbColoumnSchema> Coloumns { get; set; } = [];
    }


    [DebuggerDisplay("{Name} : {DataType}")]
    public class DbColoumnSchema
    {
        public required string Name { get; set; }
        public required string DataType { get; set; }
        public required bool IsKey { get; set; }
        public string Description { get; set; }
    }
}
