using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonControls.Common
{
    public static class PackFileUtil
    {
        public static List<PackFile> FilterUnvantedFiles(PackFileService pfs, List<PackFile> files, string[] removeFilters, out PackFile[] removedFiles)
        {
            var tempRemoveFiles = new List<PackFile>();
            var fileList = files.ToList();

            // Files that contains multiple items not decoded.
            foreach (var file in fileList)
            {
                var fullName = pfs.GetFullPath(file);
                foreach (var removeName in removeFilters)
                {
                    if (fullName.Contains(removeName))
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

        public static List<PackFile> FilterUnvantedFiles(PackFileService pfs, List<(string FileName, PackFile Pf)> files, string[] removeFilters, out PackFile[] removedFiles)
        {
            var tempRemoveFiles = new List<PackFile>();
            var fileList = files.Select(x=>x.Pf).ToList();

            // Files that contains multiple items not decoded.
            foreach (var file in files)
            {
                var fullName = file.FileName;
                foreach (var removeName in removeFilters)
                {
                    if (fullName.Contains(removeName))
                    {
                        tempRemoveFiles.Add(file.Pf);
                        break;
                    }
                }
            }

            foreach (var item in tempRemoveFiles)
                fileList.Remove(item);

            removedFiles = tempRemoveFiles.ToArray();
            return fileList;
        }

        public static string SaveFile(PackFile file, string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            DirectoryHelper.EnsureCreated(dirPath);
            var content = file.DataSource.ReadData();
            using var filestream = File.Create(path);
            filestream.Write(content);
            return path;
        }

        public static string SaveFileToTempDir(PackFile file)
        {
            var exportPath = DirectoryHelper.Temp + "\\" + file.Name;
            return SaveFile(file, exportPath);
        }
    }
}
