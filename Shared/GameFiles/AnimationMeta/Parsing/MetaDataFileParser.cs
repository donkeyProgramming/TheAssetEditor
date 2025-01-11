using System.Diagnostics;
using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public class MetaDataFileParser
    {
        private readonly ILogger _logger = Logging.Create<MetaDataFileParser>();

        public MetaDataFile? ParseFile(PackFile pf, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            if (pf == null)
                return null;
            return ParseFile(pf.DataSource.ReadData(), metaDataTagDeSerializer);
        }

        public MetaDataFile ParseFile(byte[] fileContent, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            var contentLength = fileContent.Count();

            var outputFile = new MetaDataFile()
            {
                Version = BitConverter.ToInt32(fileContent, 0)
            };

            if (outputFile.Version != 2)
                throw new Exception($"Unknown version - {outputFile.Version}");

            if (contentLength > 8)
            {
                var expectedElements = BitConverter.ToUInt32(fileContent, 4);
                var items = ExploratoryGetEntries(fileContent);

                if (Debugger.IsAttached)
                {
                    if (expectedElements != items.Count)
                        throw new Exception($"Not the expected amount elements. Expected {expectedElements}, got {items.Count}");
                }

                // Convert to sensible stuff
                foreach (var item in items)
                {
                    try
                    {
                        var deserializedTag = metaDataTagDeSerializer.DeSerialize(item, out var errorStr);
                        if (deserializedTag == null)
                        {
                            outputFile.Items.Add(item);
                            _logger.Here().Error($"Failed to parse tag of type {item.Name}_{item.Version} - {errorStr}");
                        }
                        else
                        {
                            outputFile.Items.Add(deserializedTag);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Here().Error($"Failed to parse tag of type {item.Name}_{item.Version} - {e.Message}");
                        outputFile.Items.Add(item);
                    }
                }
            }

            return outputFile;
        }

        List<UnknownMetaEntry> ExploratoryGetEntries(byte[] fileContent)
        {
            var byteLength = fileContent.Length;
            var output = new List<UnknownMetaEntry>();
            var currentIndex = 0 + 8; // version and num elements

            UnknownMetaEntry currentElement;
            while (currentIndex != byteLength && (currentElement = GetElement(currentIndex, fileContent, out currentIndex)) != null)
                output.Add(currentElement);

            return output;
        }

        public byte[] GenerateBytes(int version, IEnumerable<MetaDataTagItem> items)
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(version));
            data.AddRange(BitConverter.GetBytes(items.Count()));
            foreach (var item in items)
            {
                data.AddRange(ByteParsers.String.Encode(item.Name, out _));
                data.AddRange(item.DataItem.Bytes);
            }

            return data.ToArray();
        }

        UnknownMetaEntry GetElement(int startIndex, byte[] data, out int updatedByteIndex)
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

            var metaTagItem = new UnknownMetaEntry()
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
