using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.ByteParsing.Parsers;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation
{
    public partial class MetaDataEntry : ObservableObject
    {
        public ParsedMetadataAttribute _input;

        [ObservableProperty] ObservableCollection<AttributeViewModel> _variables = [];
        [ObservableProperty] string _displayName = "";
        [ObservableProperty] string _description = "";
        [ObservableProperty] bool _isDecodedCorrectly = false;
        [ObservableProperty] int _version;
        [ObservableProperty] bool _isSelected;

        public MetaDataEntry(ParsedMetadataAttribute typedMetaItem, string description, IEventHub eventHub, bool decodedCorrectly)
        {
            _input = typedMetaItem;
            DisplayName = typedMetaItem.DisplayName;
            Description = description;
            Version = typedMetaItem.Version;
            IsDecodedCorrectly = decodedCorrectly;

            if(IsDecodedCorrectly == false)
                return;

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

                AttributeViewModel? editableItem = null;
                if (attributeInfo.DisplayOverride == MetaDataTagAttribute.DisplayType.EulerVector || value is Vector3)
                {
                    if (value is Vector3 vector3)
                        editableItem = new VectorAttributeViewModel(parser as Vector3Parser, vector3, typedMetaItem, prop, eventHub);
                    else if (value is Vector4 quaternion)
                        editableItem = new OrientationAttributeViewModel(parser as Vector4Parser, quaternion, typedMetaItem, prop, eventHub);
                    else
                        throw new Exception("Unknown item");
                }
                else
                {
                    editableItem = new AttributeViewModel(parser, value.ToString(), typedMetaItem, prop, eventHub);
                }

                editableItem.Description = itemDiscription;
                editableItem.FieldName = FormatFieldName(prop.Name);
                editableItem.IsReadOnly = !attributeInfo.IsEditable;
                Variables.Add(editableItem);
            }

            if (Variables.Count != 0)
                Variables.First().IsReadOnly = true;
        }

        public string? HasError()
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

