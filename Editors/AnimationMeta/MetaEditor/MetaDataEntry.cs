using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.ByteParsing.Parsers;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation
{
    public abstract partial class IMetaDataEntry : ObservableObject
    {
        public ParsedMetadataAttribute _input;

        [ObservableProperty] ObservableCollection<MetaDataAttribute> _variables  = [];

        [ObservableProperty] string _displayName = "";
        [ObservableProperty] string _description = "";
        [ObservableProperty] bool _isDecodedCorrectly = false;
        [ObservableProperty] int _version;
        [ObservableProperty] bool _isSelected;

        public abstract string HasError();
    }

    public class UnkMetaDataEntry : IMetaDataEntry
    {
        private readonly ParsedUnknownMetadataAttribute _input;

        public UnkMetaDataEntry(ParsedUnknownMetadataAttribute unknownMeta)
        {
            _input = unknownMeta;

            IsDecodedCorrectly = false;
            DisplayName = unknownMeta.DisplayName;
            Version = unknownMeta.Version;
        }

        public override string HasError() => "";
    }

    public class MetaDataEntry : IMetaDataEntry
    {
        private readonly string _originalName;
    

        public MetaDataEntry(ParsedMetadataAttribute typedMetaItem, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _input = typedMetaItem;
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
                        editableItem = new VectorMetaDataAttribute(parser as Vector3Parser, vector3, typedMetaItem, prop);
                    else if (value is Vector4 quaternion)
                        editableItem = new OrientationMetaDataAttribute(parser as Vector4Parser, quaternion, typedMetaItem, prop);
                    else
                        throw new Exception("Unknown item");
                }
                else
                {
                    editableItem = new MetaDataAttribute(parser, value.ToString(), typedMetaItem, prop);
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

