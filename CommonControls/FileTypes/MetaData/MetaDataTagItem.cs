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
        public bool DecodedCorrectly { get; }
        public byte[] GetData();
    }

    [DebuggerDisplay("{Name}_v{Version} [Unkn]")]
    public class UnknownMetaEntry : IMetaEntry
    {
        byte[] _data;

        public string Name { get; private set; }
        public int Version { get; private set; }
        public bool DecodedCorrectly => false;
        public byte[] GetData() => _data;

        public UnknownMetaEntry(string name, int version, byte[] data)
        {
            Name = name;
            Version = version;
            _data = data;
        }
    }

    [DebuggerDisplay("{Name}_v{Version}")]
    public class MetaEntry : IMetaEntry
    {
        byte[] _data;

        public string Name { get; private set; }
        public int Version { get; private set; }
        public bool DecodedCorrectly => true;

        public DbTableDefinition Schema { get; set; }

        public MetaEntry(string name, int version, byte[] data, DbTableDefinition schema)
        {
            Name = name;
            Version = version;
            _data = data;
            Schema = schema;
        }

        public MetaEntry(DbTableDefinition schema)
        {
            Schema = schema;
            Name = schema.TableName;
            Version = schema.Version;

            var bytes = new List<byte>();
            foreach (var field in Schema.ColumnDefinitions)
            {
                var parser = ByteParserFactory.Create(field.Type);
                var defaultValue = parser.DefaultValue();
                if (field.Name.ToLower() == "version")
                    defaultValue = schema.Version.ToString();

                var fieldBytes = parser.Encode(defaultValue, out var error);
                if (fieldBytes == null)
                    throw new Exception("Failed to create meta tag with default values : " + error);
                bytes.AddRange(fieldBytes);
            }

            _data = bytes.ToArray();

        }

        public byte[] GetData() => _data;

        public bool Validate()
        {
            try
            {
                var chuck = new ByteChunk(GetData());
                foreach (var field in Schema.ColumnDefinitions)
                {
                    var parser = ByteParserFactory.Create(field.Type);
                    chuck.Read(parser, out var value, out var error);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

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
        [DebuggerDisplay("{Name}_v{Version}")]
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

        public string Name { get; set; } = "";
        public int Version { get; set; }
        public TagData DataItem { get; set; }
        public bool IsDecodedCorrectly { get; set; } = false;
    }
}

/*
 
  public class EditableTagItem : NotifyPropertyChangedImpl
    {
        byte[] _originalByteValue;
        IByteParser _parser { get; set; }

        public EditableTagItem(IByteParser parser, byte[] value)
        {
            _originalByteValue = value;
            _parser = parser;
            IsValid = _parser.TryDecode(_originalByteValue, 0, out _valueAsString, out _, out _);
        }

        string _valueAsString;
        public string ValueAsString { get => _valueAsString; set { SetAndNotify(ref _valueAsString, value); Validate(); } }

        public string FieldName { get; set; }
        public string Description { get; set; }
        public string ValueType { get; set; }

        bool _isValueValid;
        public bool IsValid { get => _isValueValid; set { SetAndNotify(ref _isValueValid, value); } }

        void Validate()
        {
            IsValid = _parser.Encode(ValueAsString, out _) != null;
        }

        public byte[] GetByteValue()
        {
            return _parser.Encode(ValueAsString, out _);
        }

        public override string ToString()
        {
            return $"{FieldName} - {ValueAsString} - {IsValid}";
        }
    }
 */
