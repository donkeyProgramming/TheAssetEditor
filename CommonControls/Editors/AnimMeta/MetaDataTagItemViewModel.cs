using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CommonControls.Editors.AnimMeta
{
    public class MetaDataTagItemViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<MetaDataTagItemViewModel>();
        IMetaEntry _originalItem;
        string _originalName;

        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> Description { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> IsDecodedCorrectly { get; set; } = new NotifyAttr<bool>(false);

        public MetaDataTagItemViewModel(IMetaEntry item)
        {
            _originalItem = item;
            _originalName = item.Name;
            DisplayName.Value = $"{item.Name}_v{item.Version}";
            Description.Value = MetaEntrySerializer.GetDescriptionSafe(_originalName);

            try
            {
                var typedMetaItem = MetaEntrySerializer.DeSerialize(item);
                var orderedPropertiesList = typedMetaItem.GetType().GetProperties()
                           .Where(x => x.CanWrite)
                           .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                           .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order);

                foreach (var prop in orderedPropertiesList)
                {
                    var attributeInfo = prop.GetCustomAttributes<MetaDataTagAttribute>(false).Single();
                    var parser = ByteParserFactory.Create(prop.PropertyType);
                    var value = prop.GetValue(typedMetaItem);
                    var itemDiscription = $"Value type is {prop.PropertyType.Name}";
                    if (string.IsNullOrWhiteSpace(attributeInfo.Description) == false)
                        itemDiscription = attributeInfo.Description + "\n" + itemDiscription;

                    EditableTagItem editableItem = null;
                    if (attributeInfo.DisplayOverride == MetaDataTagAttribute.DisplayType.EulerVector || value is Vector3)
                    {
                        if (value is Vector3 vector3)
                            editableItem = new Vector3EditableTagItem(parser as Vector3Parser, vector3);
                        else if (value is Vector4 quaternion)
                            editableItem = new OrientationEditableTagItem(parser as Vector4Parser, quaternion);
                        else
                            throw new Exception("Unkown item");
                    }
                    else
                    {
                        editableItem = new EditableTagItem(parser, value.ToString());
                    }

                    editableItem.Description = itemDiscription;
                    editableItem.FieldName = FormatFieldName(prop.Name);
                    editableItem.IsReadOnly = !attributeInfo.IsEditable;
                    Variables.Add(editableItem);
                }

                IsDecodedCorrectly.Value = true;
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                IsDecodedCorrectly.Value = false;
            }
        }

        string FormatFieldName(string name)
        {
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i != 0)
                    newName += " ";
                newName += name[i];
            }
            return newName;
        }


        public MetaDataTagItemViewModel(MetaEntryBase typedMetaItem, string displayName)
        {
            DisplayName.Value = displayName;
            var splitString = displayName.Split("_");
            var newName = string.Join("_", splitString.SkipLast(1));
            _originalName = newName;
            Description.Value = MetaEntrySerializer.GetDescriptionSafe(_originalName);

            try
            {
                var orderedPropertiesList = typedMetaItem.GetType().GetProperties()
                           .Where(x => x.CanWrite)
                           .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                           .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order);

                foreach (var prop in orderedPropertiesList)
                {
                    var attributeInfo = prop.GetCustomAttributes<MetaDataTagAttribute>(false).Single();
                    var parser = ByteParserFactory.Create(prop.PropertyType);
                    var value = prop.GetValue(typedMetaItem);
                    var itemDiscription = $"Value type is {prop.PropertyType.Name}";
                    if (string.IsNullOrWhiteSpace(attributeInfo.Description) == false)
                        itemDiscription = attributeInfo.Description + "\n" + itemDiscription;

                    EditableTagItem editableItem = null;
                    if (attributeInfo.DisplayOverride == MetaDataTagAttribute.DisplayType.EulerVector || value is Vector3)
                    {
                        if (value is Vector3 vector3)
                            editableItem = new Vector3EditableTagItem(parser as Vector3Parser, vector3);
                        else if (value is Vector4 quaternion)
                            editableItem = new OrientationEditableTagItem(parser as Vector4Parser, quaternion);
                        else
                            throw new Exception("Unkown item");
                    }
                    else
                    {
                        editableItem = new EditableTagItem(parser, value.ToString());
                    }

                    editableItem.Description = itemDiscription;
                    editableItem.FieldName = FormatFieldName(prop.Name);
                    editableItem.IsReadOnly = !attributeInfo.IsEditable;
                    Variables.Add(editableItem);
                }

                IsDecodedCorrectly.Value = true;
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                IsDecodedCorrectly.Value = false;
            }
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
                Name = _originalName,
            };

            if (IsDecodedCorrectly.Value == false)
            {
                _logger.Here().Information("Creating from original data");
                if (_originalItem == null)
                    throw new Exception("_originalItem is null and IsDecodedCorrectly is false");

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

