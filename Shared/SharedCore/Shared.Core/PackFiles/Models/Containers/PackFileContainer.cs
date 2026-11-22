using CommunityToolkit.Diagnostics;
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
        private string? _systemFilePath;

        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsReadOnly { get { return IsCaPackFile || field; } set; } = false;
        public bool IsCaPackFile { get; set; } = false;
        public string? SystemFilePath
        {
            get => _systemFilePath;
            set
            {
                _systemFilePath = value;
                PackFileSettings.SaveLocationPath = value;
            }
        }
        public PackFileSettings PackFileSettings { get; } = new();
        public PackFileContainerType ContainerType => PackFileContainerType.Normal;
        public long OriginalLoadByteSize { get; set; } = -1;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];
        
        public Dictionary<string, PackFile> FileList { get; set; } = [];

        // The file AE last wrote and how it looked afterwards. A save over anything else (the pack as
        // it was opened or reloaded) is the first one, the one that keeps a backup.
        private (string Path, PackFileStamp? Stamp)? _lastSave;

        protected PackFileContainer(string name, PackFileVersion version)
        {
            Name = name;
            var versionStr = PackFileVersionConverter.ToString(version);
            Header = new PFHeader(versionStr, PackFileCAType.MOD);
        }

        public void SaveSettings()
        {
            PackFileSettings.Save();
        }

        // Notes the size and write time of each pack the files are read from, so reading or saving
        // fails with a clear error once another program (e.g. RPFM) rewrites it, instead of reading stale offsets
        internal void TrackChangesOnDisk()
        {
            foreach (var parent in GetPackedFileSourceParents())
                parent.ExpectedStamp = PackFileStamp.Read(parent.FilePath);
        }

        internal bool HasChangedOnDisk() => GetPackedFileSourceParents().Any(x => x.HasChangedOnDisk);

        // Files whose content isn't read from this pack's file: edited, added or copied in AE since the last save
        internal List<PackFile> GetUnsavedFiles()
        {
            var isReadFromThisPack = CreateIsReadFromThisPack();
            return FileList.Values.Where(x => isReadFromThisPack(x) == false).ToList();
        }

        private Func<PackFile, bool> CreateIsReadFromThisPack()
        {
            if (string.IsNullOrWhiteSpace(SystemFilePath))
                return _ => false;

            var packPath = Path.GetFullPath(SystemFilePath);
            return file => file.DataSource is PackedFileSource packed
                && string.Equals(Path.GetFullPath(packed.Parent.FilePath), packPath, StringComparison.OrdinalIgnoreCase);
        }

        // Re-reads the pack after another program (e.g. RPFM) saved it. Files AE has edited, added or copied
        // but not saved stay on top of the new version; everything else now reads from the new file. PackFile
        // instances are kept for paths that still exist, so open editors keep valid references. Renames and
        // deletions AE hasn't saved are undone, since the new version still has those files.
        // Returns the unsaved files that were kept.
        internal List<PackFile> ReloadFromDisk()
        {
            Guard.IsNotNullOrWhiteSpace(SystemFilePath, "Only a pack opened from disk can be reloaded");

            var isReadFromThisPack = CreateIsReadFromThisPack();
            var unsaved = FileList.Where(x => isReadFromThisPack(x.Value) == false).ToList();
            var unsavedPaths = unsaved.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            PackFileContainer fresh;
            using (var stream = File.OpenRead(SystemFilePath))
            using (var reader = new BinaryReader(stream, System.Text.Encoding.ASCII))
                fresh = PackFileSerializerLoader.Load(SystemFilePath, stream.Length, reader, new RecordingDuplicateFileResolver());

            var reloaded = new Dictionary<string, PackFile>(fresh.FileList.Count + unsaved.Count);
            foreach (var (path, freshFile) in fresh.FileList)
            {
                if (unsavedPaths.Contains(path))
                    continue;

                if (FileList.TryGetValue(path, out var existing))
                {
                    existing.DataSource = freshFile.DataSource;
                    reloaded[path] = existing;
                }
                else
                {
                    reloaded[path] = freshFile;
                }
            }

            foreach (var (path, file) in unsaved)
                reloaded[path] = file;

            FileList = reloaded;
            Header = fresh.Header;
            OriginalLoadByteSize = fresh.OriginalLoadByteSize;
            TrackChangesOnDisk();

            // The other program's version is now the one to keep on the next save
            _lastSave = null;

            return unsaved.Select(x => x.Value).ToList();
        }

        // Why PackFileSerializerWriter can't write this pack back, or null if it can.
        // Checked when a pack is opened for editing, so nobody finds out on the first save.
        internal string? WhyCannotBeSaved()
        {
            if (Header.HasEncryptedIndex || Header.HasEncryptedData)
                return "it is encrypted";
            if (Header.Version == PackFileVersion.PFH6)
                return "it is a PFH6 pack, which AssetEditor can read but not write";
            if (Header.HasExtendedHeader)
                return "its header has an extended section AssetEditor can't write";
            return null;
        }

        private IEnumerable<PackedFileSourceParent> GetPackedFileSourceParents()
        {
            return FileList.Values
                .Select(x => x.DataSource)
                .OfType<PackedFileSource>()
                .Select(x => x.Parent)
                .Distinct();
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

        public string? GetExactFullPath(PackFile file)
        {
            var pathByReference = FileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            return string.IsNullOrWhiteSpace(pathByReference) ? null : pathByReference;
        }

        public virtual string? GetFullPath(PackFile file)
        {
            var pathByReference = GetExactFullPath(file);
            if (pathByReference != null)
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
            var effectiveGameInformation = PackFileSettings.GameVersion.HasValue
                ? GameInformationDatabase.GetGameById(PackFileSettings.GameVersion.Value)
                : gameInformation;

            if (Header.Version != effectiveGameInformation.PackFileVersion)
            {
                _logger.Here().Information("Overriding pack header version from {CurrentVersion} to {TargetVersion} based on PackFileSettings.GameVersion", Header.Version, effectiveGameInformation.PackFileVersion);
                Header.Version = effectiveGameInformation.PackFileVersion;
            }

            _logger.Here().Information("Saving pack file to disk at {Path}, createBackup = {createBackup} Game = {game}", path, createBackup, effectiveGameInformation.DisplayName);

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

            // Files still read from the packs they were loaded from; a rewritten pack would feed the save garbage
            foreach (var parent in GetPackedFileSourceParents())
                parent.EnsureUnchanged();

            // createBackup keeps the file being replaced as <name>.pack1, but only on the first save since
            // the pack was opened or reloaded: later saves would only back up what AE itself wrote
            var backupPath = createBackup && File.Exists(path) && IsFirstSaveOver(path)
                ? SaveUtility.GetPackBackupPath(path)
                : null;

            if (OriginalLoadByteSize != -1)
            {
                Guard.IsNotNull(SystemFilePath, "SystemFilePath must be set if saving to disk");
                var fileInfo = new FileInfo(SystemFilePath);
                var byteSize = fileInfo.Length;
                if (byteSize != OriginalLoadByteSize)
                    throw new Exception("File has been changed outside of AssetEditor. Can not save the file as it will cause corruptions");
            }

            var sourceUpdates = SaveUtility.WriteThroughTempFile(path,
                writer => PackFileSerializerWriter.SaveToByteArray(path, this, writer, effectiveGameInformation),
                backupPath);
            _logger.Here().Information(backupPath == null
                ? "Pack file written and moved into place"
                : $"Pack file written and moved into place, the previous version kept as '{backupPath}'");

            // Only now does the file at path hold the offsets the new sources point at
            foreach (var update in sourceUpdates)
                update.PackFile.DataSource = update.NewSource;
            TrackChangesOnDisk();

            SystemFilePath = path;
            OriginalLoadByteSize = new FileInfo(path).Length;
            _lastSave = (Path.GetFullPath(path), PackFileStamp.Read(path));

            _logger.Here().Information("SaveToDisk completed");
        }

        private bool IsFirstSaveOver(string path)
        {
            return _lastSave is not { } last
                || string.Equals(last.Path, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) == false
                || last.Stamp != PackFileStamp.Read(path);
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
