using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation
{
    public abstract partial class IMetaDataEntry : ObservableObject
    {
        [ObservableProperty] ObservableCollection<MetaDataAttribute> _variables  = [];

        [ObservableProperty] string _displayName = "";
        [ObservableProperty] string _description = "";
        [ObservableProperty] bool _isDecodedCorrectly = false;
        [ObservableProperty] int _version;
        [ObservableProperty] bool _isSelected;

        public abstract MetaDataTagItem GetAsFileFormatData();
        public abstract string HasError();
    }

    public class UnkMetaDataEntry : IMetaDataEntry
    {
        private readonly UnknownMetaEntry _input;

        public UnkMetaDataEntry(UnknownMetaEntry unknownMeta)
        {
            _input = unknownMeta;

            IsDecodedCorrectly = false;
            DisplayName = unknownMeta.DisplayName;
            Version = unknownMeta.Version;
        }

        public override MetaDataTagItem GetAsFileFormatData()
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

    public class MetaDataEntry : IMetaDataEntry
    {
        private readonly ILogger _logger = Logging.Create<MetaDataEntry>();
        private readonly string _originalName;

        public MetaDataEntry(BaseMetaEntry typedMetaItem, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _originalName = typedMetaItem.Name;
            DisplayName = typedMetaItem.DisplayName;
            Description = metaDataTagDeSerializer.GetDescriptionSafe(_originalName);
            Version = typedMetaItem.Version;

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

                MetaDataAttribute editableItem = null;
                if (attributeInfo.DisplayOverride == MetaDataTagAttribute.DisplayType.EulerVector || value is Vector3)
                {
                    if (value is Vector3 vector3)
                        editableItem = new VectorMetaDataAttribute(parser as Vector3Parser, vector3);
                    else if (value is Vector4 quaternion)
                        editableItem = new OrientationMetaDataAttribute(parser as Vector4Parser, quaternion);
                    else
                        throw new Exception("Unknown item");
                }
                else
                {
                    editableItem = new MetaDataAttribute(parser, value.ToString());
                }

                editableItem.Description = itemDiscription;
                editableItem.FieldName = FormatFieldName(prop.Name);
                editableItem.IsReadOnly = !attributeInfo.IsEditable;
                Variables.Add(editableItem);
            }

            IsDecodedCorrectly = true;

            if (Variables.Count != 0)
                Variables.First().IsReadOnly = true;
        }

        public override string HasError()
        {
            foreach (var variable in Variables)
            {
                if (!variable.IsValid)
                    return $"Variable '{variable.FieldName}' in {DisplayName} has an error";
            }

            return null;
        }

        public override MetaDataTagItem GetAsFileFormatData()
        {
            _logger.Here().Information("Start " + DisplayName);
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

            var currentByte = 0;
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

        static string FormatFieldName(string name)
        {
            var newName = "";
            for (var i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i != 0)
                    newName += " ";
                newName += name[i];
            }
            return newName;
        }
    }
}

