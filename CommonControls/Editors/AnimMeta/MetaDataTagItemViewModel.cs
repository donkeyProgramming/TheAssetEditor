using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using Filetypes.ByteParsing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CommonControls.Editors.AnimMeta
{
    public class MetaDataTagItemViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<MetaDataTagItemViewModel>();
        IMetaEntry _originalItem;

        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> IsDecodedCorrectly { get; set; } = new NotifyAttr<bool>(false);


        public MetaDataTagItemViewModel(IMetaEntry item)
        {
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

                if (field.Name.Contains("orientation", StringComparison.InvariantCultureIgnoreCase))
                {
                    var item = new OrientationEditableTagItem(ByteParsers.Vector4, byteValue)
                    {
                        Description = field.Description,
                        FieldName = $"[{counter + 1}] {field.Name} - { field.Type}",
                        ValueType = field.Type.ToString(),
                    };
                    output.Add(item);
                }
                else if (field.Type == DbTypesEnum.Vector3)
                {
                    var item = new Vector3EditableTagItem(ByteParsers.Vector3, byteValue)
                    {
                        Description = field.Description,
                        FieldName = $"[{counter + 1}] {field.Name} - { field.Type}",
                        ValueType = field.Type.ToString(),
                    };
                    output.Add(item);
                }
                else
                {
                    EditableTagItem item = new EditableTagItem(parser, byteValue)
                    {
                        Description = field.Description,
                        FieldName = $"[{counter + 1}] {field.Name} - { field.Type}",
                        ValueType = field.Type.ToString(),
                    };
                    output.Add(item);
                }
                totalBytesRead += fieldByteLength;
                counter++;
            }

            return output;
        }

        public string HasError()
        {
            foreach (var variable in Variables)
            {
                if (!variable.IsValid)
                    return $"Variable '{variable.FieldName}' in {DisplayName} has an error";
            }

            return null;
        }


        internal MetaDataTagItem GetAsData()
        {
            _logger.Here().Information("Start " + DisplayName.Value);
            var newItem = new MetaDataTagItem()
            {
                Name = _originalItem.Name,
                Version = _originalItem.Version
            };

            if (!IsDecodedCorrectly.Value)
            {
                _logger.Here().Information("Creating from original data");
                if (_originalItem == null)
                {
                    _logger.Here().Error("Data missing!");
                    throw new Exception("_originalItem is null and IsDecodedCorrectly is false");
                }

                _logger.Here().Information("Getting data");
                var data = _originalItem.GetData();
                _logger.Here().Information("Creating tag. Length=" + data.Length);
                var copy = new MetaDataTagItem.TagData(data, 0, data.Length);
                newItem.DataItem = copy;
                return newItem;
            }

            _logger.Here().Information("Getting variables");
            var byteList = new List<byte[]>();
            foreach (var variable in Variables)
            {
                _logger.Here().Information(variable.FieldName + " " + variable.ValueAsString);
                var bytes = variable.GetByteValue();
                _logger.Here().Information(variable.FieldName + " " + variable.ValueAsString + " {" + bytes.Length + "}");
                byteList.Add(bytes);
            }

            _logger.Here().Information("Creating byte array");
            var totalCount = byteList.Sum(x => x.Length);
            var byteArray = new byte[totalCount];

            int currentByte = 0;
            foreach (var byteItem in byteList)
            {
                byteItem.CopyTo(byteArray, currentByte);
                currentByte += byteItem.Length;
            }

            _logger.Here().Information("Creating tag. Length=" + totalCount);
            var instance = new MetaDataTagItem.TagData(byteArray, 0, totalCount);
            newItem.DataItem = instance;

            _logger.Here().Information("Done");
            return newItem;
        }
    }
}

