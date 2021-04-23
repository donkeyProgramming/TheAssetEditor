using AnimMetaEditor.DataType;
using Common;
using CommonControls;
using Filetypes.ByteParsing;
using System;
using System.Collections.ObjectModel;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MetaDataTagItemViewModel : NotifyPropertyChangedImpl
    {
        MetaDataTagItem _originalItem;
        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        bool _IsDecodedCorrectly;
        public bool IsDecodedCorrectly { get => _IsDecodedCorrectly; set => SetAndNotify(ref _IsDecodedCorrectly, value); }

        public MetaDataTagItemViewModel(MetaDataTagItem item, SchemaManager schemaManager)
        {
            _originalItem = item;

            DisplayName = $"{_originalItem.Name}_v{_originalItem.Version}";
            IsDecodedCorrectly = item.IsDecodedCorrectly;
            if (IsDecodedCorrectly)
            {
                var schema = schemaManager.GetMetaDataDefinition(item.Name, item.Version);
                var fields = schema.ColumnDefinitions;

                var totalBytesRead = 0;
                for (int i = 0; i < fields.Count; i++)
                {
                    FileTypes.DB.DbColumnDefinition field = fields[i];
                    var parser = ByteParserFactory.Create(field.Type);
                    var result = parser.TryDecode(item.DataItems[0].Bytes, item.DataItems[0].Start + totalBytesRead, out string value, out var bytesRead, out var error);

                    var byteValue = new byte[bytesRead];
                    Array.Copy(item.DataItems[0].Bytes, item.DataItems[0].Start + totalBytesRead, byteValue, 0, bytesRead);
                    EditableTagItem item222 = new EditableTagItem(parser, byteValue)
                    {
                        Description = field.Description,
                        FieldName = $"[{i + 1}] {field.Name} - { field.Type}",
                        ValueType = field.Type.ToString(),
                    };

                    Variables.Add(item222);

                    totalBytesRead += bytesRead;
                }
            }
        }

    }
}

