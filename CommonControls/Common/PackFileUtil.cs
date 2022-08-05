using CommonControls.FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Common
{
    public static class PackFileUtil
    {
        public static List<PackFile> FilterUnvantedFiles(List<PackFile> files, string[] removeFilters, out PackFile[] removedFiles)
        {
            var tempRemoveFiles = new List<PackFile>();
            var fileList = files.ToList();

            // Files that contains multiple items not decoded.
            foreach (var file in fileList)
            {
                foreach (var removeName in removeFilters)
                {
                    if (file.Name.Contains(removeName))
                    {
                        tempRemoveFiles.Add(file);
                        break;
                    }
                }
            }

            foreach (var item in tempRemoveFiles)
                fileList.Remove(item);

            removedFiles = tempRemoveFiles.ToArray();
            return fileList;
        }
    }
}
