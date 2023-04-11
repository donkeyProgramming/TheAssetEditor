using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using MoreLinq;
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

        public static Dictionary<string, PackFile> FilterUnvantedFiles(Dictionary<string,PackFile> files, string[] removeFilters, out string[] removedFiles)
        {
            var tempRemoveFiles = new List<string>();
            var fileList = files.ToDictionary();  // Create a copy

            foreach (var file in files)
            {
                var fullName = file.Key;
                foreach (var removeName in removeFilters)
                {
                    if (fullName.Contains(removeName))
                    {
                        tempRemoveFiles.Add(file.Key);
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

        public static PackFileService CreatePackFileServiceFromSystemFile(string path)
        {
            if (File.Exists(path) == false)
                throw new Exception();

            var pfs = CreatePackFileService();
            var container = pfs.GetAllPackfileContainers().First();
            pfs.AddFileToPack(container, "systemfile", new PackFile(Path.GetFileName(path), new FileSystemSource(path)));

            return pfs;
        }

        public static PackFileService CreatePackFileService()
        {

            var pfs = new PackFileService(new PackFileDataBase(), new SkeletonAnimationLookUpHelper(), new ApplicationSettingsService());
            var container = pfs.CreateNewPackFileContainer("temp", PackFileCAType.MOD);
            pfs.SetEditablePack(container);

            return pfs;
        }

        public static PackFile CreateNewPackFileFromSystem(string filePath, out string directoryPath)
        {
            var fileSource = new FileSystemSource(filePath);
            var packfile = new PackFile(Path.GetFileName(filePath), fileSource);
            directoryPath = Path.GetDirectoryName(filePath);
            return packfile;
        }
    }
}
