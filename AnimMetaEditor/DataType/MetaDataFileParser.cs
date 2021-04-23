using Common;
using CommonControls.Services;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimMetaEditor.DataType
{
    static class MetaDataFileParser
    {
        public static MetaDataFile ParseFile(PackFile file, PackFileService pf)
        {
            var fileContent = file.DataSource.ReadData();
            var contentLength = fileContent.Count();


            MetaDataFile outputFile = new MetaDataFile()
            {
                FileName = pf.GetFullPath(file),
                Version = BitConverter.ToInt32(fileContent, 0)
            };

            if (outputFile.Version != 2)
                throw new Exception($"Unknown version - {outputFile.Version} for {outputFile.FileName}");

            if (contentLength > 8)
            {
                MetaDataTagItem currentElement = null;
                int numElements = BitConverter.ToInt32(fileContent, 4);
                int currentIndex = 0 + 8; // First 4 bytes is the number of elements, next 2 is unknown
                while (currentIndex != contentLength && (currentElement = GetElement(currentIndex, fileContent, outputFile.FileName, out currentIndex)) != null)
                    outputFile.TagItems.Add(currentElement);

                if (numElements != outputFile.TagItems.Count)
                    throw new Exception($"Not the expected amount elements. Expected {numElements}, got {outputFile.TagItems.Count}");
            }

            return outputFile;
        }


        static MetaDataTagItem GetElement(int startIndex, byte[] data, string parentFileName, out int updatedByteIndex)
        {
            if(! ByteParsers.String.TryDecode(data, startIndex, out var tagName, out var strBytesRead, out string error))
                throw new Exception($"Unable to detect tagname for MetaData element starting at {startIndex} - {error}");

            int currentIndex = startIndex + strBytesRead;

            for (; currentIndex < data.Length; currentIndex++)
            {
                if (IsAllCapsCaString(currentIndex, data))
                    break;
            }

            var metaTagItem = new MetaDataTagItem()
            {
                Name = tagName,
            };

            var start = startIndex + strBytesRead;
            updatedByteIndex = currentIndex;

            var dataItem = new MetaDataTagItem.Data(parentFileName, data, startIndex + strBytesRead, currentIndex - start);

            metaTagItem.DataItems.Add(dataItem);
            metaTagItem.Version = dataItem.Version;
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
                var allCaps = tagName.All(c => char.IsUpper(c) || c == '_');
                return allCaps;
            }

            return false;
        }
        
    }
}
