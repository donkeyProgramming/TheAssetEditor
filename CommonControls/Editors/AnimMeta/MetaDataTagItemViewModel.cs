using Common;
using CommonControls.Common;
using Filetypes.ByteParsing;
using FileTypes.DB;
using FileTypes.MetaData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CommonControls.Editors.AnimMeta
{
    public class MetaDataTagItemViewModel : NotifyPropertyChangedImpl
    {
        IMetaEntry _originalItem;
        SchemaManager _schemaManager;

        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> IsDecodedCorrectly { get; set; } = new NotifyAttr<bool>(false);


        public MetaDataTagItemViewModel(IMetaEntry item, SchemaManager schemaManager)
        {
            _schemaManager = schemaManager;
            _originalItem = item;

            DisplayName.Value = $"{item.Name}_v{item.Version}";
            IsDecodedCorrectly.Value = item.DecodedCorrectly;

            if (IsDecodedCorrectly.Value)
                 Variables = CreateVariableList(item as MetaEntry);
        }


        ObservableCollection<EditableTagItem> CreateVariableList(MetaEntry entry)
        {
            var output = new ObservableCollection<EditableTagItem>();

            var data = entry.GetData();
            var totalBytesRead = 0;
            int counter = 0;

            foreach(var field in entry.Schema.ColumnDefinitions)
            {
                var parser = ByteParserFactory.Create(field.Type);
                parser.TryDecode(data, totalBytesRead, out var _, out var fieldByteLength, out var _);

                var byteValue = new byte[fieldByteLength];
                Array.Copy(data, totalBytesRead, byteValue, 0, fieldByteLength);

                EditableTagItem item = new EditableTagItem(parser, byteValue)
                {
                    Description = field.Description,
                    FieldName = $"[{counter + 1}] {field.Name} - { field.Type}",
                    ValueType = field.Type.ToString(),
                };

                // Special for vector3 and 4
                output.Add(item);
                totalBytesRead += fieldByteLength;
                counter++;
            }

            return output;
        }


        /*internal MetaDataTagItem GetAsData()
        {
            var newItem = new MetaDataTagItem()
            {
                Name = _originalItem.Name,
                Version = _originalItem.Version
            };

            if (!IsDecodedCorrectly.Value)
            {
                if (_originalItem == null)
                    throw new Exception("_originalItem is null and IsDecodedCorrectly is false");

                var tmp = new byte[_originalItem.DataItem.Size];
                Array.Copy(_originalItem.DataItem.Bytes, _originalItem.DataItem.Start, tmp, 0, _originalItem.DataItem.Size);
                var copy = new MetaDataTagItem.TagData(tmp, 0, tmp.Length);
                newItem.DataItem = copy;
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

            var instance = new MetaDataTagItem.TagData(byteArray, 0, totalCount);
            newItem.DataItem = instance;
            return newItem;
        }*/
    }
}

