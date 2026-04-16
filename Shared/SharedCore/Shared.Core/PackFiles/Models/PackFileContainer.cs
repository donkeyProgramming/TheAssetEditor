using Shared.Core.Misc;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public class PackFileContainer
    {
        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public long OriginalLoadByteSize { get; set; } = -1;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public Dictionary<string, PackFile> FileList { get; set; } = [];

        public PackFileContainer(string name)
        {
            Name = name;
        }

        public void MergePackFileContainer(PackFileContainer other)
        {
            foreach (var item in other.FileList)
                FileList[item.Key] = item.Value;
            return;
        }

        public virtual List<PackFile> AddFiles(List<NewPackFileEntry> newFiles)
        {
            foreach (var file in newFiles)
            {
                if (string.IsNullOrWhiteSpace(file.PackFile.Name))
                    throw new Exception("PackFile name can not be empty");
            }

            foreach (var file in newFiles)
            {
                file.PackFile.Name = file.PackFile.Name.Trim();

                var path = file.DirectoyPath.Trim();
                if (!string.IsNullOrWhiteSpace(path))
                    path += "\\";
                path += file.PackFile.Name;
                FileList[path.ToLower()] = file.PackFile;
            }

            return newFiles.Select(x => x.PackFile).ToList();
        }

        public virtual PackFile DeleteFile(PackFile file)
        {
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            FileList.Remove(key);
            return file;
        }

        public virtual void DeleteFolder(string folder)
        {
            var filesToDelete = new List<string>();
            foreach (var file in FileList)
            {
                var directory = Path.GetDirectoryName(file.Key);
                if (directory == null)
                    continue;

                if (directory.StartsWith(folder, StringComparison.InvariantCultureIgnoreCase))
                    filesToDelete.Add(file.Key);
            }

            foreach (var item in filesToDelete)
                FileList.Remove(item);
        }

        public virtual PackFile? FindFile(string path)
        {
            var lowerPath = path.Replace('/', '\\').ToLower().Trim();
            return FileList.TryGetValue(lowerPath, out var value) ? value : null;
        }

        public virtual string? GetFullPath(PackFile file)
        {
            var res = FileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)
                || string.Equals(x.Value.Name, file.Name, StringComparison.OrdinalIgnoreCase)).Key;
            return string.IsNullOrWhiteSpace(res) ? null : res;
        }

        public virtual void MoveFile(PackFile file, string newFolderPath)
        {
            var newFullPath = newFolderPath + "\\" + file.Name;
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            FileList.Remove(key);
            FileList[newFullPath] = file;
        }

        public virtual string RenameDirectory(string currentNodeName, string newName)
        {
            var oldNodePath = currentNodeName;
            var newNodePath = newName;
            var lastSeparatorIndex = currentNodeName.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSeparatorIndex != -1)
            {
                var parentPath = currentNodeName.Substring(0, lastSeparatorIndex);
                newNodePath = parentPath + Path.DirectorySeparatorChar + newName;
            }

            var oldPathPrefix = oldNodePath + Path.DirectorySeparatorChar;
            var files = FileList
                .Where(x => x.Key.Equals(oldNodePath, StringComparison.InvariantCultureIgnoreCase)
                            || x.Key.StartsWith(oldPathPrefix, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var (path, file) in files)
            {
                FileList.Remove(path);
                var newPath = newNodePath;
                if (oldNodePath.Length != 0 && path.Length > oldNodePath.Length)
                    newPath = newNodePath + path.Substring(oldNodePath.Length);

                FileList[newPath] = file;
            }

            return newNodePath;
        }

        public virtual void RenameFile(PackFile file, string newName)
        {
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            FileList.Remove(key);

            var dir = Path.GetDirectoryName(key);
            file.Name = newName;
            FileList[dir + "\\" + file.Name] = file;
        }

        public virtual void SaveFileData(PackFile file, byte[] data)
        {
            file.DataSource = new MemorySource(data);
        }

        public virtual void SaveToDisk(string path, bool createBackup, GameInformation gameInformation)
        {
            if (File.Exists(path) && DirectoryHelper.IsFileLocked(path))
                throw new IOException($"Cannot access {path} because another process has locked it, most likely the game.");

            if (createBackup)
                SaveUtility.CreateFileBackup(path);

            if (OriginalLoadByteSize != -1)
            {
                var fileInfo = new FileInfo(SystemFilePath);
                var byteSize = fileInfo.Length;
                if (byteSize != OriginalLoadByteSize)
                    throw new Exception("File has been changed outside of AssetEditor. Can not save the file as it will cause corruptions");
            }

            using (var memoryStream = new FileStream(path + "_temp", FileMode.Create))
            {
                using var writer = new BinaryWriter(memoryStream);
                PackFileSerializerWriter.SaveToByteArray(path, this, writer, gameInformation);
            }

            File.Delete(path);
            File.Move(path + "_temp", path);

            SystemFilePath = path;
            OriginalLoadByteSize = new FileInfo(path).Length;
        }
    }
}
