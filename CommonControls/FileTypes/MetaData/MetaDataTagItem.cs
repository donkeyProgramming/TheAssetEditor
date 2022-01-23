using CommonControls.FileTypes.DB;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.MetaData
{
    public interface IMetaEntry
    {
        public string Name { get; }
        public int Version { get; }
        public byte[] GetData();
    }

    [DebuggerDisplay("{Name}_v{Version} [Unkn]")]
    public class UnknownMetaEntry : IMetaEntry
    {
        byte[] _data;

        public string Name { get; private set; }
        public int Version { get; private set; }
        public byte[] GetData() => _data;

        public UnknownMetaEntry(string name, int version, byte[] data)
        {
            Name = name;
            Version = version;
            _data = data;
        }
    }

    // Remove
    [DebuggerDisplay("{Name}_v{Version}")]
    public class MetaEntry : IMetaEntry
    {
        byte[] _data;

        public string Name { get; private set; }
        public int Version { get; private set; }

        public DbTableDefinition Schema { get; set; }

        public byte[] GetData() => _data;

        public T Get<T>(string name)
        {
            var fields = Schema.ColumnDefinitions.Where(x => x.Name == name);
            if (fields.Count() != 1)
                throw new Exception($"Unexpected number of fields with the given name - {fields.Count()}");

            var chuck = new ByteChunk(GetData());
            foreach (var field in Schema.ColumnDefinitions)
            {
                if (field.Name == name)
                {
                    var parser = ByteParserFactory.Create(fields.First().Type) as SpesificByteParser<T>;
                    chuck.Read(parser, out var value, out var error);
                    return value;
                }
                else
                {
                    var parser = ByteParserFactory.Create(field.Type);
                    chuck.Read(parser, out var value, out var error);
                }
            }

            throw new Exception();
        }
    }

    public class MetaDataTagItem
    {
        public class TagData
        {
            public TagData(byte[] bytes, int start, int size)
            {
                Bytes = bytes;
                Start = start;
                Size = size;
            }

            public byte[] Bytes { get; set; }
            public int Start { get; set; }
            public int Size { get; set; }
        }

        public string Name { get; set; } = ""; // Only name, no _v10 stuff here. Used for saving
        public TagData DataItem { get; set; }
        public bool IsDecodedCorrectly { get; set; } = false;
    }
}
