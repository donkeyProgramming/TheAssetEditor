using CommunityToolkit.Diagnostics;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal class PackFileContainer : IPackFileContainerInternal
    {
        private static readonly ILogger _logger = Logging.Create<PackFileContainer>();    

        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsReadOnly { get { return IsCaPackFile || field; } set; } = false;
        public bool IsCaPackFile { get; set; } = false;
        public string? SystemFilePath { get; set; }
        public PackFileContainerType ContainerType => PackFileContainerType.Normal;
        public long OriginalLoadByteSize { get; set; } = -1;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];
        
        public Dictionary<string, PackFile> FileList { get; set; } = [];

        protected PackFileContainer(string name, PackFileVersion version)
        {
            Name = name;
            var versionStr = PackFileVersionConverter.ToString(version);
            Header = new PFHeader(versionStr, PackFileCAType.MOD);
        }

        public static PackFileContainer CreateCaPackFile(string name, string? systemFilePath = null, PackFileVersion version = PackFileVersion.PFH5)
        {
            return new PackFileContainer(name, version)
            {
                IsCaPackFile = true,
                IsReadOnly = true,
                SystemFilePath = systemFilePath
            };
        }

        public static PackFileContainer CreateReadOnlyPackFile(string name, string? systemFilePath = null, PackFileVersion version = PackFileVersion.PFH5)
        {
            return new PackFileContainer(name, version)
            {
                IsCaPackFile = false,
                IsReadOnly = true,
                SystemFilePath = systemFilePath
            };
        }

        public static PackFileContainer CreatePackFile(string name, string? systemFilePath = null, PackFileVersion version = PackFileVersion.PFH5)
        {
            return new PackFileContainer(name, version)
            {
                IsCaPackFile = false,
                IsReadOnly = false,
                SystemFilePath = systemFilePath
            };
        }

        public static PackFileContainer CreatePackFile(string name, string systemFilePath, PFHeader header)
        {
            return new PackFileContainer(name, header.Version)
            {
                Header = header,
                SystemFilePath = systemFilePath
            };
        }

        public int GetFileCount() => FileList.Count;

        public void AddOrUpdateFile(string path, PackFile file)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            FileList[lowerPath] = file;
        }

        public Dictionary<string, PackFile> GetAllFiles() => FileList;

        public List<(string Path, PackFile File)> GetDirectoryContent(string directoryPath)
        {
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var results = new List<(string Path, PackFile File)>();
            var directFileSlashCount = string.IsNullOrEmpty(directoryPath) ? 0 : directoryPath.Count(c => c == '\\') + 1;

            foreach (var (path, packFile) in FileList)
            {
                if ((string.IsNullOrEmpty(prefix) || path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    && path.Count(c => c == '\\') == directFileSlashCount)
                    results.Add((path, packFile));
            }

            results.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Path, b.Path));
            return results;
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

        public List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions)
        {
            var results = new List<(string Path, PackFile File)>();

            foreach (var (path, packFile) in FileList)
            {
                if (extensions != null && extensions.Count > 0)
                {
                    var matchesExtension = false;
                    foreach (var ext in extensions)
                    {
                        if (packFile.Name.Contains(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            matchesExtension = true;
                            break;
                        }
                    }
                    if (!matchesExtension)
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(textFilter))
                {
                    if (!packFile.Name.Contains(textFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                results.Add((path, packFile));
            }

            results.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Path, b.Path));
            return results;
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

                var path = BuildPackPath(file.DirectoyPath, file.PackFile.Name);
                FileList[path] = file.PackFile;
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
            var newFullPath = BuildPackPath(newFolderPath, file.Name);
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
            var newPath = BuildPackPath(dir, file.Name);
            FileList[newPath] = file;
        }

        public virtual void SaveFileData(PackFile file, byte[] data)
        {
            file.DataSource = new MemorySource(data);
        }

        public virtual void SaveToDisk(string path, bool createBackup, GameInformation gameInformation)
        {
            _logger.Here().Information("Saving pack file to disk at {Path}, createBackup = {createBackup} Game = {game}", path, createBackup, gameInformation.DisplayName);

            if (File.Exists(path) && DirectoryHelper.IsFileLocked(path))
            {
                var msg = $"Cannot access {path} because another process has locked it, most likely the game.";
                _logger.Here().Error(msg);
                throw new IOException(msg);
            }

            if (File.Exists(path + "_temp") && DirectoryHelper.IsFileLocked(path + "_temp"))
            {
                var msg = $"Cannot access {path + "_temp"} because another process has locked it, most likely the game.";
                _logger.Here().Error(msg);
                throw new IOException(msg);
            }

            if (createBackup)
                SaveUtility.CreateFileBackup(path);

            if (OriginalLoadByteSize != -1)
            {
                Guard.IsNotNull(SystemFilePath, "SystemFilePath must be set if saving to disk");
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

            _logger.Here().Information("Finished writing pack file to temporary file, now replacing original file with the new one");
            File.Delete(path);

            _logger.Here().Information("Original file deleted, now moving temporary file to original file path");
            File.Move(path + "_temp", path);

            SystemFilePath = path;
            OriginalLoadByteSize = new FileInfo(path).Length;

            _logger.Here().Information("SaveToDisk completed");
        }

        private static string BuildPackPath(string? directoryPath, string fileName)
        {
            var normalizedFileName = fileName.Replace('/', '\\').Trim().TrimStart('\\');
            var normalizedDirectory = PathNormalization.NormalizeDirectoryPath(directoryPath);

            if (string.IsNullOrWhiteSpace(normalizedFileName))
                throw new Exception("PackFile name can not be empty");

            var fullPath = string.IsNullOrEmpty(normalizedDirectory)
                ? normalizedFileName
                : normalizedDirectory + "\\" + normalizedFileName;

            return PathNormalization.NormalizeFileName(fullPath);
        }

        public SortedDictionary<string, List<string>> GetAllFilesByFolder()
        {
            return PackFileSortHelper.BuildSortedFilesByFolder(FileList.Keys);
        }
    }
}
