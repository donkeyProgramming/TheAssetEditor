using AnimMetaEditor.DataType;
using Common;
using CommonControls;
using Filetypes.ByteParsing;
using FileTypes.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MetaDataTagItemViewModel : NotifyPropertyChangedImpl
    {
        MetaDataTagItem _originalItem;
        SchemaManager _schemaManager;
        string _name;
        int _version;

        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        bool _IsDecodedCorrectly;
        public bool IsDecodedCorrectly { get => _IsDecodedCorrectly; set => SetAndNotify(ref _IsDecodedCorrectly, value); }

        public MetaDataTagItemViewModel(MetaDataTagItem item, SchemaManager schemaManager)
        {
            _originalItem = item;
            _schemaManager = schemaManager;
            _name = _originalItem.Name;
            _version = _originalItem.Version;

            DisplayName = $"{_name}_v{_version}";
            IsDecodedCorrectly = item.IsDecodedCorrectly;
            if (IsDecodedCorrectly)
                Variables = CreateVariableList(_originalItem.DataItems[0].Bytes, _originalItem.DataItems[0].Start);
        }


        public MetaDataTagItemViewModel(DbTableDefinition definition, SchemaManager schemaManager)
        {
            _originalItem = null;
            _schemaManager = schemaManager;
            _name = definition.TableName;
            _version = definition.Version;

            DisplayName = $"{_name}_v{_version}";
            IsDecodedCorrectly = true;
            Variables = CreateVariableList(definition, _version);
        }

        ObservableCollection<EditableTagItem> CreateVariableList(DbTableDefinition definition, int version)
        {
            ObservableCollection<EditableTagItem> output = new ObservableCollection<EditableTagItem>();

            for (int i = 0; i < definition.ColumnDefinitions.Count; i++)
            {
                DbColumnDefinition variable = definition.ColumnDefinitions[i];

                var parser = ByteParserFactory.Create(variable.Type);
                var value = parser.DefaultValue();
                if (i == 0) // version field
                    value = version.ToString(); ;
                var byteValue = parser.Encode(value, out _);

                EditableTagItem item = new EditableTagItem(parser, byteValue)
                {
                    Description = variable.Description,
                    FieldName = $"[{i + 1}] {variable.Name} - { variable.Type}",
                    ValueType = variable.Type.ToString(),
                };

                output.Add(item);

            }
            return output;
        }



        ObservableCollection<EditableTagItem> CreateVariableList(byte[] data, int dataStart)
        {
            ObservableCollection<EditableTagItem> output = new ObservableCollection<EditableTagItem>();

            var schema = _schemaManager.GetMetaDataDefinition(_originalItem.Name, _originalItem.Version);
            var fields = schema.ColumnDefinitions;

            var totalBytesRead = 0;
            for (int i = 0; i < fields.Count; i++)
            {
                FileTypes.DB.DbColumnDefinition field = fields[i];
                var parser = ByteParserFactory.Create(field.Type);
                var result = parser.TryDecode(data, dataStart + totalBytesRead, out string value, out var bytesRead, out var error);

                var byteValue = new byte[bytesRead];
                Array.Copy(data, dataStart + totalBytesRead, byteValue, 0, bytesRead);
                EditableTagItem item = new EditableTagItem(parser, byteValue)
                {
                    Description = field.Description,
                    FieldName = $"[{i + 1}] {field.Name} - { field.Type}",
                    ValueType = field.Type.ToString(),
                };

                output.Add(item);

                totalBytesRead += bytesRead;
            }

            return output;
        }


        internal MetaDataTagItem ConvertToData()
        {
            var newItem = new MetaDataTagItem()
            {
                Name = _name,
                Version = _version
            };

            if (!IsDecodedCorrectly)
            {
                if (_originalItem == null)
                    throw new Exception("_originalItem is null and IsDecodedCorrectly is false");

                var tmp = new byte[_originalItem.DataItems[0].Size];
                Array.Copy(_originalItem.DataItems[0].Bytes, _originalItem.DataItems[0].Start, tmp, 0, _originalItem.DataItems[0].Size);
                var copy = new MetaDataTagItem.Data(_originalItem.DataItems[0].ParentFileName, tmp, 0, tmp.Length);
                newItem.DataItems.Add(copy);
                return newItem;
            }

            var byteList = new List<byte[]>();
            foreach (var variable in Variables)
                byteList.Add(variable.GetByteValue());

            var totalCount = byteList.Sum(x => x.Length);
            var byteArray = new byte[totalCount];

            int currentByte = 0;
            foreach (var byteItem in byteList)
            {
                byteItem.CopyTo(byteArray, currentByte);
                currentByte += byteItem.Length;

            }

            var instance = new MetaDataTagItem.Data("", byteArray, 0, totalCount);
            newItem.DataItems.Add(instance);

            //Validate(totalCount, instance);
            
            return newItem;
        }

        private void Validate(int totalCount, MetaDataTagItem.Data instance)
        {
            if (_originalItem != null)
            {
                if (_originalItem.DataItems[0].Size != totalCount)
                    throw new Exception("Different sizes");

                for (int i = 0; i < totalCount; i++)
                {
                    var orgVal = _originalItem.DataItems[0].Bytes[_originalItem.DataItems[0].Start + i];
                    var newValue = instance.Bytes[i];

                    if (orgVal != newValue)
                        throw new Exception("Different values at index = " + i.ToString());
                }
            }
        }

    }
}

