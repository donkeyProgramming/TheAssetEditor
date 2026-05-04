using Shared.Core.Misc;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class PackFileContainer : IPackFileContainerInternal
    {
        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public long OriginalLoadByteSize { get; set; } = -1;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        public Dictionary<string, PackFile> FileList { get; set; } = [];

        public int GetFileCount() => FileList.Count;

        public void AddOrUpdateFile(string path, PackFile file)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            FileList[lowerPath] = file;
        }

        public Dictionary<string, PackFile> GetAllFiles() => FileList;

        public DirectoryContent GetDirectoryContent(string directoryPath)
        {
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var prefixLength = prefix.Length;
            var subFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new List<(string FileName, PackFile File)>();

            foreach (var (path, packFile) in FileList)
            {
                if (prefixLength > 0 && !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (prefixLength == 0 && path.Length == 0)
                    continue;

                var remainder = path.AsSpan(prefixLength);
                var separatorIndex = remainder.IndexOf(Path.DirectorySeparatorChar);

                if (separatorIndex == -1)
                {
                    files.Add((packFile.Name, packFile));
                }
                else
                {
                    var folderName = remainder.Slice(0, separatorIndex).ToString();
                    subFolders.Add(folderName);
                }
            }

            return new DirectoryContent
            {
                SubFolders = subFolders.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList(),
                Files = files.OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase).ToList()
            };
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            var output = new List<(string, PackFile)>();
            foreach (var file in FileList)
            {
                if (Path.GetExtension(file.Key) == extention)
                    output.Add((file.Key, file.Value));
            }
            return output;
        }

        public PackFileContainer(string name)
        {
            Name = name;
        }

        public void MergePackFileContainer(PackFileContainer other)
        {
            foreach (var item in other.GetAllFiles())
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

        public virtual PackFile? DeleteFile(PackFile file)
        {
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            if (key == null)
                return null;
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

                if (directory.Equals(folder, StringComparison.InvariantCultureIgnoreCase)
                    || directory.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.InvariantCultureIgnoreCase))
                    filesToDelete.Add(file.Key);
            }

            foreach (var item in filesToDelete)
                FileList.Remove(item);
        }

        public virtual PackFile? FindFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            return FileList.TryGetValue(lowerPath, out var value) ? value : null;
        }

        public virtual bool ContainsFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            return FileList.ContainsKey(lowerPath);
        }

        public virtual string? GetFullPath(PackFile file)
        {
            var pathByReference = FileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (!string.IsNullOrWhiteSpace(pathByReference))
                return pathByReference;

            var pathByName = FileList.FirstOrDefault(x => string.Equals(x.Value.Name, file.Name, StringComparison.OrdinalIgnoreCase)).Key;
            return string.IsNullOrWhiteSpace(pathByName) ? null : pathByName;
        }

        public virtual void MoveFile(PackFile file, string newFolderPath)
        {
            var newFullPath = newFolderPath + "\\" + file.Name;
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            FileList.Remove(key);
            FileList[newFullPath.ToLower()] = file;
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

                FileList[newPath.ToLower()] = file;
            }

            return newNodePath;
        }

        public virtual void RenameFile(PackFile file, string newName)
        {
            var key = FileList.FirstOrDefault(x => x.Value == file).Key;
            FileList.Remove(key);

            var dir = Path.GetDirectoryName(key);
            file.Name = newName;
            var newPath = string.IsNullOrEmpty(dir) ? file.Name : dir + "\\" + file.Name;
            FileList[newPath.ToLower()] = file;
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
