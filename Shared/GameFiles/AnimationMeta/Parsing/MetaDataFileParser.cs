using System.Diagnostics;
using Serilog;
using Shared.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public class MetaDataFileParser
    {
        private readonly ILogger _logger = Logging.Create<MetaDataFileParser>();

        public ParsedMetadataFile? ParseFile(PackFile pf, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            if (pf == null)
                return null;
            return ParseFile(pf.DataSource.ReadData(), metaDataTagDeSerializer);
        }

        public ParsedMetadataFile ParseFile(byte[] fileContent, MetaDataTagDeSerializer metaDataTagDeSerializer)
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
                    var deserializedAttribute = metaDataTagDeSerializer.DeSerialize(attribute, out var errorStr);
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
            var metaDataTagDeSerializer = new MetaDataTagDeSerializer();
        
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(metaFile.Version));
            data.AddRange(BitConverter.GetBytes(metaFile.Attributes.Count()));
            foreach (var item in metaFile.Attributes)
            {
                var bytes = metaDataTagDeSerializer.Serialize(item, out var errorStr)   ;


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
                if (IsAllCapsCaString(currentIndex, data))
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

        bool IsAllCapsCaString(int index, byte[] data)
        {
            if (ByteParsers.String.TryDecode(data, index, out var tagName, out _, out _))
            {
                if (string.IsNullOrWhiteSpace(tagName))
                    return false;
                if (tagName.Length < 4)
                    return false;
                var allCaps = tagName.All(c => char.IsUpper(c) || c == '_' || c == ' ' || char.IsNumber(c));
                return allCaps;
            }

            return false;
        }
    }
}
