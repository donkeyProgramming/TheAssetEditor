using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.DB
{
    [DebuggerDisplay("{Name} - {Type}")]
    public class DbColumnDefinition : ICloneable
    {
        public string Name { get; set; }
        public string FieldReference { get; set; }
        public string TableReference { get; set; }
        public bool IsKey { get; set; } = false;
        public bool IsOptional { get; set; }
        public int MaxLength { get; set; }
        public bool IsFileName { get; set; } = false;
        public string Description { get; set; }
        public string FilenameRelativePath { get; set; }


        [JsonConverter(typeof(StringEnumConverter))]
        public DbTypesEnum Type { get; set; }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
