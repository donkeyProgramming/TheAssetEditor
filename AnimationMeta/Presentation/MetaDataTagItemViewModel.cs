using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AnimationMeta.FileTypes.Parsing;
using CommonControls.Common;
using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;
using Serilog;

namespace AnimationMeta.Presentation
{

    public abstract class MetaTagViewBase : NotifyPropertyChangedImpl
    {
        public ObservableCollection<EditableTagItem> Variables { get; set; } = new ObservableCollection<EditableTagItem>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> Description { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<bool> IsDecodedCorrectly { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<int> Version { get; set; } = new NotifyAttr<int>();

        public abstract MetaDataTagItem GetAsData();
        public abstract string HasError();
    }

    public class UnkMetaDataTagItemViewModel : MetaTagViewBase
    {
        UnknownMetaEntry _input;

        public UnkMetaDataTagItemViewModel(UnknownMetaEntry unknownMeta)
        {
            IsDecodedCorrectly.Value = false;
            DisplayName.Value = unknownMeta.DisplayName;
            Version.Value = unknownMeta.Version;
            _input = unknownMeta;
        }

        public override MetaDataTagItem GetAsData()
        {
            var newItem = new MetaDataTagItem()
            {
                Name = _input.Name,
                DataItem = new MetaDataTagItem.TagData(_input.Data, 0, _input.Data.Length)
            };
            return newItem;
        }

        public override string HasError() => "";
    }


    public class MetaDataTagItemViewModel : MetaTagViewBase
    {
        ILogger _logger = Logging.Create<MetaDataTagItemViewModel>();
        string _originalName;

        public MetaDataTagItemViewModel(BaseMetaEntry typedMetaItem)
        {
            DisplayName.Value = typedMetaItem.DisplayName;
            _originalName = typedMetaItem.Name;
            Description.Value = MetaDataTagDeSerializer.GetDescriptionSafe(_originalName);
            Version.Value = typedMetaItem.Version;

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
                        throw new Exception("Unknown item");
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

            if (Variables.Count() != 0)
                Variables.First().IsReadOnly = true;
        }

        public override string HasError()
        {
            foreach (var variable in Variables)
            {
                if (!variable.IsValid)
                    return $"Variable '{variable.FieldName}' in {DisplayName.Value} has an error";
            }

            return null;
        }

        public override MetaDataTagItem GetAsData()
        {
            _logger.Here().Information("Start " + DisplayName.Value);
            var newItem = new MetaDataTagItem()
            {
                Name = _originalName,
            };

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

            _logger.Here().Information("Creating tag. Length = " + totalCount);
            var instance = new MetaDataTagItem.TagData(byteArray, 0, totalCount);
            newItem.DataItem = instance;

            _logger.Here().Information("Done");
            return newItem;
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
    }
}

