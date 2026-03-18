using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using Serilog;
using Shared.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public class MetaDataFileParser
    {
        private readonly ILogger _logger = Logging.Create<MetaDataFileParser>();
        private readonly IMetaDataDatabase _metaDataDatabase;

        public MetaDataFileParser(IMetaDataDatabase metaDataDatabase)
        {
            _metaDataDatabase = metaDataDatabase;
        }

        public IMetaDataDatabase GetDatabase() => _metaDataDatabase;

        public ParsedMetadataFile? ParseFile(PackFile pf)
        {
            if (pf == null)
                return null;
            return ParseFile(pf.DataSource.ReadData());
        }

        public ParsedMetadataFile ParseFile(byte[] fileContent)
        {
            var contentLength = fileContent.Length;

            var outputFile = new ParsedMetadataFile()
            {
                Version = BitConverter.ToInt32(fileContent, 0)
            };

            if (outputFile.Version != 2)
                throw new Exception($"Unknown version - {outputFile.Version}");

            if (contentLength > 8 == false)
                return outputFile;

            var expectedAttributeCount = BitConverter.ToUInt32(fileContent, 4);
            var attributes = GetAttributes(fileContent);
            Debug.Assert(expectedAttributeCount == attributes.Count, $"Not the expected amount elements. Expected {expectedAttributeCount}, got {attributes.Count}");

            // Try to convert from UnkownAttribute to KnownAttribute.
            // If this fails, we keep the unkown in the list as it has the correct byte data. 
            foreach (var attribute in attributes)
            {
                try
                {
                    var deserializedAttribute = DeSerialize(attribute, out var errorStr);
                    if (deserializedAttribute != null)
                    {
                        outputFile.Attributes.Add(deserializedAttribute);
                    }
                    else
                    {
                        outputFile.Attributes.Add(attribute);
                        _logger.Here().Error($"Failed to parse tag of type {attribute.Name}_{attribute.Version} - {errorStr}");
                    }
                }
                catch (Exception e)
                {
                    _logger.Here().Error($"Failed to parse tag of type {attribute.Name}_{attribute.Version} - {e.Message}");
                    outputFile.Attributes.Add(attribute);
                }
            }
            
            return outputFile;
        }

        public byte[] GenerateBytes(int version, ParsedMetadataFile metaFile)
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(metaFile.Version));
            data.AddRange(BitConverter.GetBytes(metaFile.Attributes.Count()));
            foreach (var item in metaFile.Attributes)
            {
                var bytes = Serialize(item, out var errorStr)   ;
                data.AddRange(ByteParsers.String.Encode(item.Name, out _));
                data.AddRange(bytes);
            }

            return data.ToArray();
        }

        List<ParsedUnknownMetadataAttribute> GetAttributes(byte[] fileContent)
        {
            var byteLength = fileContent.Length;
            var output = new List<ParsedUnknownMetadataAttribute>();
            var currentIndex = 0 + 8; // version and num elements

            ParsedUnknownMetadataAttribute currentElement;
            while (currentIndex != byteLength && (currentElement = GetAttribute(currentIndex, fileContent, out currentIndex)) != null)
                output.Add(currentElement);

            return output;
        }


        ParsedUnknownMetadataAttribute GetAttribute(int startIndex, byte[] data, out int updatedByteIndex)
        {
            if (!ByteParsers.String.TryDecode(data, startIndex, out var tagName, out var strBytesRead, out var error))
                throw new Exception($"Unable to detect tagname for MetaData element starting at {startIndex} - {error}");

            var currentIndex = startIndex + strBytesRead;

            for (; currentIndex < data.Length; currentIndex++)
            {
                if (StringSanitizer.IsAllCapsCaString(currentIndex, data))
                    break;
            }

            updatedByteIndex = currentIndex;

            var start = startIndex + strBytesRead;
            var size = currentIndex - start;

            var version = BitConverter.ToInt32(data, startIndex + strBytesRead);

            var destination = new byte[size];
            Array.Copy(data, start, destination, 0, size);

            var metaTagItem = new ParsedUnknownMetadataAttribute()
            {
                Name = tagName,
                Version = version,
                Data = destination
            };

            return metaTagItem;
        }


        public ParsedMetadataAttribute? DeSerialize(ParsedUnknownMetadataAttribute entry, out string? errorMessage)
        {
            var possibleClassLayouts = GetPossibleClassLayoutsForMetaDataAttribute(entry);
            if (possibleClassLayouts == null)
            {
                errorMessage = $"Unable to find decoder for deserializing {entry.Name}_{entry.Version}";
                return null;
            }

            errorMessage = null;

            // Try all possible class layouts until we find one that can successfully read the data
            foreach (var possibleClassLayout in possibleClassLayouts)
            {
                var attributeInstance = Activator.CreateInstance(possibleClassLayout.Type);
                var bytes = entry.Data;
                var currentIndex = 0;
                foreach (var proptery in possibleClassLayout.Properties)
                {
                    var parser = ByteParserFactory.Create(proptery.PropertyType);
                    try
                    {
                        var value = parser.GetValueAsObject(bytes, currentIndex, out var bytesRead);
                        currentIndex += bytesRead;
                        proptery.SetValue(attributeInstance, value);
                        errorMessage = "";
                    }
                    catch (Exception e)
                    {
                        errorMessage = $"Failed to read object - {e.Message} bytes left";
                        break;
                    }
                }

                if (errorMessage != "")
                    continue;

                var bytesLeft = bytes.Length - currentIndex;
                if (bytesLeft != 0)
                {
                    errorMessage = $"Failed to read object - {bytesLeft} bytes left";
                    continue;
                }

                var typedInstance = attributeInstance as ParsedMetadataAttribute;
                Guard.IsNotNull(typedInstance, $"Failed to convert {attributeInstance} into {nameof(ParsedMetadataAttribute)}");

                typedInstance.Name = entry.Name;
                typedInstance.Data = bytes;
                errorMessage = null;
                return typedInstance;
            }

            return null;
        }


        public byte[]? Serialize(ParsedMetadataAttribute entry, out string? errorMessage)
        {
            var classLayout = GetClassLayoutsForMetaDataAttribute(entry);
            if (classLayout == null)
            {
                errorMessage = $"Unable to find decoder for serializing {entry.Name}_{entry.Version}";
                return null;
            }

            // Convert the class to bytes by getting the class layout and serializing each property in order.
            var data = new List<byte>();
            foreach (var proptery in classLayout.Properties)
            {
                var propertyValue = ReflectionHelper.GetMemberValue(entry, proptery.Name);
                var parser = ByteParserFactory.Create(proptery.PropertyType);
                var attributeByteValue = parser.Encode(propertyValue);
                data.AddRange(attributeByteValue);
            }

            errorMessage = null;
            return data.ToArray();
        }




        /// <summary>
        /// Get all possible metadata definitions for a given metadata attribute.
        /// It could be multiple, as some games share the same attribute name, but 
        /// has different binary representation. For example BlendOverride_v11_Troy
        /// </summary>
        public List<Type> GetPossibleTypesForMetaDataAttribute(ParsedMetadataAttribute entry)
        {
            var key = entry.Name + "_" + entry.Version;
            var s = _metaDataDatabase.GetDefinition(key);
            return s;

        }

        public List<(string Header, string Value)>? DeSerializeToStrings(ParsedMetadataAttribute entry, out string? errorMessage)
        {
            var entryInfoList = GetPossibleClassLayoutsForMetaDataAttribute(entry);
            if (entryInfoList == null)
            {
                errorMessage = $"Unable to find decoder for {entry.Name}_{entry.Version}";
                return null;
            }

            var bytes = entry.Data;
            var currentIndex = 0;
            var output = new List<(string, string)>();
            errorMessage = null;

            foreach (var entryInfo in entryInfoList)
            {
                errorMessage = "";
                foreach (var proptery in entryInfo.Properties)
                {
                    var parser = ByteParserFactory.Create(proptery.PropertyType);
                    var result = parser.TryDecode(bytes, currentIndex, out var value, out var bytesRead, out var error);
                    if (result == false)
                    {
                        errorMessage = $"Failed to serialize {proptery.Name} - {error}";
                        break;
                    }
                    currentIndex += bytesRead;
                    output.Add((proptery.Name, value));
                }

                if (errorMessage != "")
                    continue;

                var bytesLeft = bytes.Length - currentIndex;
                if (bytesLeft != 0)
                {
                    errorMessage = $"Failed to read object - {bytesLeft} bytes left";
                    continue;
                }

                errorMessage = null;
                return output;
            }

            return output;
        }

        public ParsedMetadataAttribute CreateDefault(string itemName)
        {
            var type = _metaDataDatabase.GetDefinition(itemName);
            if (type.Count == 0)
                throw new Exception("Unknown metadata item " + itemName);

            var instance = Activator.CreateInstance(type.First()) as ParsedMetadataAttribute;

            var itemNameSplit = itemName.ToUpper().Split("_");
            instance.Version = int.Parse(itemNameSplit.Last());
            instance.Name = string.Join("_", itemNameSplit.Take(itemNameSplit.Length - 1));
            return instance;
        }



        List<EntryInfoResult>? GetPossibleClassLayoutsForMetaDataAttribute(ParsedMetadataAttribute entry)
        {
            var possibleAttributeTypes = GetPossibleTypesForMetaDataAttribute(entry);
            if (possibleAttributeTypes.Count() == 0)
                return null;

            var output = new List<EntryInfoResult>();
            foreach (var possibleAttributeType in possibleAttributeTypes)
            {
                var instance = Activator.CreateInstance(possibleAttributeType);
                var orderedPropertiesList = possibleAttributeType.GetProperties()
                    .Where(x => x.CanWrite)
                    .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                    .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                    .ToList();

                var entryInfo = new EntryInfoResult(possibleAttributeType, orderedPropertiesList);
                output.Add(entryInfo);
            }

            return output;
        }

        EntryInfoResult GetClassLayoutsForMetaDataAttribute(ParsedMetadataAttribute entry)
        {
            var orderedPropertiesList = entry.GetType().GetProperties()
                .Where(x => x.CanWrite)
                .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                .ToList();

            var entryInfo = new EntryInfoResult(entry.GetType(), orderedPropertiesList);
            return entryInfo;
        }

        record EntryInfoResult(Type? Type, List<PropertyInfo> Properties);


    }
}
