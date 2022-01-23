using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.PackFiles.Models;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.MetaData
{
    public static class MetaDataFileParser
    {
       
        public static MetaDataFile ParseFile(byte[] fileContent)
        {
            var contentLength = fileContent.Count();

            MetaDataFile outputFile = new MetaDataFile()
            {
                Version = BitConverter.ToInt32(fileContent, 0)
            };

            if (outputFile.Version != 2)
                throw new Exception($"Unknown version - {outputFile.Version}");

            if (contentLength > 8)
            {
                int numElements = BitConverter.ToInt32(fileContent, 4);
                var items = ExploratoryGetEntries(fileContent);

                if (numElements != items.Count)
                    throw new Exception($"Not the expected amount elements. Expected {numElements}, got {items.Count}");

                // Convert to sensible stuff
                foreach (var item in items)
                    outputFile.Items.Add(item);
            }

            return outputFile;
        }

        static List<UnknownMetaEntry> ExploratoryGetEntries(byte[] fileContent)
        {
            int byteLength = fileContent.Length;
            var output = new List<UnknownMetaEntry>();
            UnknownMetaEntry currentElement = null;
            int currentIndex = 0 + 8; // version and num elements

            while (currentIndex != byteLength && (currentElement = GetElement(currentIndex, fileContent, out currentIndex)) != null)
                output.Add(currentElement);

            return output;
        }

        public static byte[] GenerateBytes(int version, IEnumerable<MetaDataTagItem> items)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(version));
            data.AddRange(BitConverter.GetBytes(items.Count()));
            foreach (var item in items)
            {
                data.AddRange(ByteParsers.String.Encode(item.Name, out _));
                data.AddRange(item.DataItem.Bytes);
            }

            return data.ToArray();
        }

        static UnknownMetaEntry GetElement(int startIndex, byte[] data, out int updatedByteIndex)
        {
            if (!ByteParsers.String.TryDecode(data, startIndex, out var tagName, out var strBytesRead, out string error))
                throw new Exception($"Unable to detect tagname for MetaData element starting at {startIndex} - {error}");

            int currentIndex = startIndex + strBytesRead;

            for (; currentIndex < data.Length; currentIndex++)
            {
                if (IsAllCapsCaString(currentIndex, data))
                    break;
            }

            updatedByteIndex = currentIndex;

            var start = startIndex + strBytesRead;
            var size = currentIndex - start;

            var version = BitConverter.ToInt32(data, startIndex + strBytesRead);

            byte[] destination = new byte[size];
            Array.Copy(data, start, destination, 0, size);

            var metaTagItem = new UnknownMetaEntry(tagName, version, destination);

            return metaTagItem;
        }

        static bool IsAllCapsCaString(int index, byte[] data)
        {
            if (ByteParsers.String.TryDecode(data, index, out var tagName, out _, out _))
            {
                if (string.IsNullOrWhiteSpace(tagName))
                    return false;
                if (tagName.Length < 4)
                    return false;
                var allCaps = tagName.All(c => char.IsUpper(c) || c == '_' || c == ' ');
                return allCaps;
            }

            return false;
        }
    }
}
