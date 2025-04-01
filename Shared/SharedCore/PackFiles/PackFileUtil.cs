using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public static class PackFileUtil
    {
        public static List<PackFile> FilterUnvantedFiles(IPackFileService pfs, List<PackFile> files, string[] removeFilters, out PackFile[] removedFiles)
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

        public static Dictionary<string, PackFile> FilterUnvantedFiles(Dictionary<string, PackFile> files, string[] removeFilters, out string[] removedFiles)
        {
            var tempRemoveFiles = new List<string>();
            var fileList = files.ToDictionary(entry => entry.Key, entry => entry.Value); ;  // Create a copy

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

        public static List<PackFile> LoadFilesFromDisk(IPackFileService pfs, IEnumerable<FileRef> fileRefs)
        {
            var packFileList = new List<NewPackFileEntry>();
            foreach (var fileRef in fileRefs)
            {
                var fileSource = new FileSystemSource(fileRef.SystemPath);
                var packfileName = fileRef.OverrideName;
                if (packfileName == null)
                    packfileName = Path.GetFileName(fileRef.SystemPath);
                var packfile = new PackFile(packfileName, fileSource);

                packFileList.Add( new NewPackFileEntry(fileRef.PackFilePath, packfile));
            }

            pfs.AddFilesToPack(pfs.GetEditablePack(), packFileList);
            return packFileList.Select(x=>x.PackFile).ToList();
        }

        public static List<PackFile> LoadFileFromDisk(IPackFileService pfs, FileRef fileRef) => LoadFilesFromDisk(pfs, new FileRef[] { fileRef });

        public class FileRef
        {
            public string SystemPath { get; set; }
            public string PackFilePath { get; set; }
            public string OverrideName { get; set; } = null;

            public FileRef(string systemPath, string packFilePath)
            {
                SystemPath = systemPath;
                PackFilePath = packFilePath;
            }

            public FileRef(string systemPath, string packFilePath, string overrideName)
            {
                SystemPath = systemPath;
                PackFilePath = packFilePath;
                OverrideName = overrideName;
            }
        }
    }
}
