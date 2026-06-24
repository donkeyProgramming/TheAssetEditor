using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    public class SystemFolderContainer : IPackFileContainerInternal, IDisposable
    {
        private static readonly ILogger _logger = Logging.Create<SystemFolderContainer>();

        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemWatcher? _watcher;
        private readonly IGlobalEventHub? _eventHub;
        private readonly SynchronizationContext? _syncContext;
        private readonly Dictionary<string, PackFile> _fileList = new();
        private readonly List<FileSystemEventArgs> _pendingEvents = new();
        private Timer? _debounceTimer;
        internal volatile bool _suppressWatcher = false;
        private bool _disposed = false;

        public string Name { get; set; }
        public bool IsReadOnly { get; set; } = false;
        public bool IsCaPackFile { get; set; } = false;
        public string? SystemFilePath { get; }
        public PackFileContainerType ContainerType => PackFileContainerType.SystemFolder;

        public SystemFolderContainer(string folderPath, IFileSystemAccess fileSystemAccess, IFileSystemWatcher? watcher = null, IGlobalEventHub? eventHub = null, SynchronizationContext? syncContext = null)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));
            if (!fileSystemAccess.DirectoryExists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            _fileSystemAccess = fileSystemAccess;
            _watcher = watcher;
            _eventHub = eventHub;
            _syncContext = syncContext ?? SynchronizationContext.Current;
            SystemFilePath = folderPath;
            Name = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            ScanFolder();
            StartWatching();
        }

        private void ScanFolder()
        {
            var files = _fileSystemAccess.DirectoryGetFiles(SystemFilePath!, "*.*", SearchOption.AllDirectories);
            foreach (var absolutePath in files)
            {
                var relativePath = Path.GetRelativePath(SystemFilePath!, absolutePath);
                var normalizedPath = PathNormalization.NormalizeFileName(relativePath);
                var fileName = Path.GetFileName(absolutePath);
                var packFile = new PackFile(fileName, new FileSystemSource(absolutePath));
                _fileList[normalizedPath] = packFile;
            }
        }

        // --- IPackFileContainer read operations ---

        public int GetFileCount() => _fileList.Count;

        public PackFile? FindFile(string path)
        {
            var normalizedPath = PathNormalization.NormalizeFileName(path);
            return _fileList.TryGetValue(normalizedPath, out var value) ? value : null;
        }

        public bool ContainsFile(string path)
        {
            var normalizedPath = PathNormalization.NormalizeFileName(path);
            return _fileList.ContainsKey(normalizedPath);
        }

        public string? GetFullPath(PackFile file)
        {
            var pathByReference = _fileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (!string.IsNullOrWhiteSpace(pathByReference))
                return pathByReference;

            // Fallback: match by name, but only when it is unambiguous. Matching by name
            // when several files share the same name could resolve to the wrong file.
            var matchesByName = _fileList.Where(x => string.Equals(x.Value.Name, file.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            return matchesByName.Count == 1 ? matchesByName[0].Key : null;
        }

        public Dictionary<string, PackFile> GetAllFiles() => _fileList;

        public SortedDictionary<string, List<string>> GetAllFilesByFolder()
        {
            return PackFileSortHelper.BuildSortedFilesByFolder(_fileList.Keys);
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            var output = new List<(string, PackFile)>();
            foreach (var file in _fileList)
            {
                if (Path.GetExtension(file.Key) == extention)
                    output.Add((file.Key, file.Value));
            }
            return output;
        }

        public List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions)
        {
            var results = new List<(string Path, PackFile File)>();

            foreach (var (path, packFile) in _fileList)
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

            results.Sort((a, b) => PackFileSortHelper.PathComparer.Compare(a.Path, b.Path));
            return results;
        }

        public List<(string Path, PackFile File)> GetDirectoryContent(string directoryPath)
        {
            var prefix = string.IsNullOrEmpty(directoryPath) ? "" : directoryPath + "\\";
            var results = new List<(string Path, PackFile File)>();
            var directFileSlashCount = string.IsNullOrEmpty(directoryPath) ? 0 : directoryPath.Count(c => c == '\\') + 1;

            foreach (var (path, packFile) in _fileList)
            {
                if ((string.IsNullOrEmpty(prefix) || path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    && path.Count(c => c == '\\') == directFileSlashCount)
                    results.Add((path, packFile));
            }

            results.Sort((a, b) => PackFileSortHelper.PathComparer.Compare(a.Path, b.Path));
            return results;
        }

        // --- IPackFileContainerInternal write operations ---

        public void AddOrUpdateFile(string path, PackFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
                throw new Exception("PackFile name can not be empty");

            var normalizedPath = PathNormalization.NormalizeFileName(path);
            var absolutePath = Path.Combine(SystemFilePath!, normalizedPath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystemAccess.DirectoryExists(directory))
                _fileSystemAccess.DirectoryCreateDirectory(directory);

            var data = file.DataSource.ReadData();
            using (SuppressWatcher())
                _fileSystemAccess.FileWriteAllBytes(absolutePath, data);

            var newPackFile = new PackFile(file.Name, new FileSystemSource(absolutePath));
            _fileList[normalizedPath] = newPackFile;
        }

        public List<PackFile> AddFiles(List<NewPackFileEntry> newFiles)
        {
            foreach (var file in newFiles)
            {
                if (string.IsNullOrWhiteSpace(file.PackFile.Name))
                    throw new Exception("PackFile name can not be empty");
            }

            var added = new List<PackFile>();
            using (SuppressWatcher())
            {
                foreach (var file in newFiles)
                {
                    var fileName = file.PackFile.Name.Trim();
                    var normalizedDir = PathNormalization.NormalizeDirectoryPath(file.DirectoyPath);
                    var normalizedPath = string.IsNullOrEmpty(normalizedDir)
                        ? PathNormalization.NormalizeFileName(fileName)
                        : PathNormalization.NormalizeFileName(normalizedDir + "\\" + fileName);

                    var absolutePath = Path.Combine(SystemFilePath!, normalizedPath);
                    var directory = Path.GetDirectoryName(absolutePath);
                    if (!string.IsNullOrEmpty(directory) && !_fileSystemAccess.DirectoryExists(directory))
                        _fileSystemAccess.DirectoryCreateDirectory(directory);

                    var data = file.PackFile.DataSource.ReadData();
                    _fileSystemAccess.FileWriteAllBytes(absolutePath, data);

                    var newPackFile = new PackFile(fileName, new FileSystemSource(absolutePath));
                    _fileList[normalizedPath] = newPackFile;
                    added.Add(newPackFile);
                }
            }

            return added;
        }

        public PackFile? DeleteFile(PackFile file)
        {
            var key = _fileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (key == null)
                return null;

            var absolutePath = Path.Combine(SystemFilePath!, key);
            using (SuppressWatcher())
            {
                if (_fileSystemAccess.FileExists(absolutePath))
                    _fileSystemAccess.FileDelete(absolutePath);
            }

            _fileList.Remove(key);
            return file;
        }

        public void DeleteFolder(string folder)
        {
            var normalizedFolder = PathNormalization.NormalizeFileName(folder);
            // Guard against empty / root input which would otherwise resolve to the
            // source folder itself and recursively delete the entire container.
            if (string.IsNullOrWhiteSpace(normalizedFolder) || normalizedFolder.Trim('\\').Length == 0)
                return;
            var absoluteFolderPath = Path.Combine(SystemFilePath!, normalizedFolder);

            using (SuppressWatcher())
            {
                if (_fileSystemAccess.DirectoryExists(absoluteFolderPath))
                    _fileSystemAccess.DirectoryDelete(absoluteFolderPath, true);
            }

            var keysToRemove = _fileList.Keys
                .Where(k => k.Equals(normalizedFolder, StringComparison.InvariantCultureIgnoreCase)
                         || k.StartsWith(normalizedFolder + "\\", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
                _fileList.Remove(key);
        }

        public void MoveFile(PackFile file, string newFolderPath)
        {
            var oldKey = _fileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (oldKey == null)
                throw new Exception($"File '{file.Name}' not found in container.");

            var normalizedDir = PathNormalization.NormalizeDirectoryPath(newFolderPath);
            var newRelativePath = string.IsNullOrEmpty(normalizedDir)
                ? PathNormalization.NormalizeFileName(file.Name)
                : PathNormalization.NormalizeFileName(normalizedDir + "\\" + file.Name);

            var oldAbsolutePath = Path.Combine(SystemFilePath!, oldKey);
            var newAbsolutePath = Path.Combine(SystemFilePath!, newRelativePath);

            var newDirectory = Path.GetDirectoryName(newAbsolutePath);
            if (!string.IsNullOrEmpty(newDirectory) && !_fileSystemAccess.DirectoryExists(newDirectory))
                _fileSystemAccess.DirectoryCreateDirectory(newDirectory);

            using (SuppressWatcher())
                _fileSystemAccess.FileMove(oldAbsolutePath, newAbsolutePath);

            // Keep the caller's PackFile instance so events that publish 'file'
            // can still be resolved by reference (avoids ambiguous name matching).
            _fileList.Remove(oldKey);
            file.DataSource = new FileSystemSource(newAbsolutePath);
            _fileList[newRelativePath] = file;
        }

        public string RenameDirectory(string currentNodeName, string newName)
        {
            var oldNodePath = PathNormalization.NormalizeFileName(currentNodeName);
            var newNodePath = newName;
            var lastSeparatorIndex = currentNodeName.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSeparatorIndex != -1)
            {
                var parentPath = currentNodeName.Substring(0, lastSeparatorIndex);
                newNodePath = parentPath + Path.DirectorySeparatorChar + newName;
            }
            newNodePath = PathNormalization.NormalizeFileName(newNodePath);

            var oldAbsolutePath = Path.Combine(SystemFilePath!, oldNodePath);
            var newAbsolutePath = Path.Combine(SystemFilePath!, newNodePath);

            using (SuppressWatcher())
                _fileSystemAccess.DirectoryMove(oldAbsolutePath, newAbsolutePath);

            var oldPathPrefix = oldNodePath + "\\";
            var filesToUpdate = _fileList
                .Where(x => x.Key.Equals(oldNodePath, StringComparison.InvariantCultureIgnoreCase)
                         || x.Key.StartsWith(oldPathPrefix, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var (path, packFile) in filesToUpdate)
            {
                _fileList.Remove(path);
                var newPath = newNodePath;
                if (path.Length > oldNodePath.Length)
                    newPath = newNodePath + path.Substring(oldNodePath.Length);
                newPath = PathNormalization.NormalizeFileName(newPath);

                var newFileAbsolutePath = Path.Combine(SystemFilePath!, newPath);
                var updatedPackFile = new PackFile(packFile.Name, new FileSystemSource(newFileAbsolutePath));
                _fileList[newPath] = updatedPackFile;
            }

            return newNodePath;
        }

        public void RenameFile(PackFile file, string newName)
        {
            var key = _fileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (key == null)
                throw new Exception($"File '{file.Name}' not found in container.");

            var dir = Path.GetDirectoryName(key);
            var newRelativePath = string.IsNullOrEmpty(dir)
                ? PathNormalization.NormalizeFileName(newName)
                : PathNormalization.NormalizeFileName(dir + "\\" + newName);

            var oldAbsolutePath = Path.Combine(SystemFilePath!, key);
            var newAbsolutePath = Path.Combine(SystemFilePath!, newRelativePath);

            using (SuppressWatcher())
                _fileSystemAccess.FileMove(oldAbsolutePath, newAbsolutePath);

            // Keep the caller's PackFile instance so events that publish 'file'
            // can still be resolved by reference (avoids ambiguous name matching).
            _fileList.Remove(key);
            file.Name = newName;
            file.DataSource = new FileSystemSource(newAbsolutePath);
            _fileList[newRelativePath] = file;
        }

        public void SaveFileData(PackFile file, byte[] data)
        {
            var key = _fileList.FirstOrDefault(x => ReferenceEquals(x.Value, file)).Key;
            if (key == null)
                throw new Exception($"File '{file.Name}' not found in container.");

            var absolutePath = Path.Combine(SystemFilePath!, key);
            using (SuppressWatcher())
                _fileSystemAccess.FileWriteAllBytes(absolutePath, data);

            file.DataSource = new FileSystemSource(absolutePath);
        }

        public void SaveToDisk(string path, bool createBackup, GameInformation gameInformation)
        {
            if (_fileSystemAccess.FileExists(path) && DirectoryHelper.IsFileLocked(path))
                throw new IOException($"Cannot access {path} because another process has locked it.");

            var tempPath = path + "_temp";
            if (_fileSystemAccess.FileExists(tempPath) && DirectoryHelper.IsFileLocked(tempPath))
                throw new IOException($"Cannot access {tempPath} because another process has locked it.");

            if (createBackup)
                SaveUtility.CreateFileBackup(path);

            // Build a transient PackFileContainer with in-memory data for serialization
            var versionString = PackFileVersionConverter.ToString(PackFileVersion.PFH5);
            var transientContainer = PackFileContainer.CreatePackFile(Name, path, PackFileVersion.PFH5);

            foreach (var (relativePath, packFile) in _fileList)
            {
                var data = packFile.DataSource.ReadData();
                var memFile = new PackFile(packFile.Name, new MemorySource(data));
                transientContainer.AddOrUpdateFile(relativePath, memFile);
            }

            using (SuppressWatcher())
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    using var writer = new BinaryWriter(fileStream);
                    PackFileSerializerWriter.SaveToByteArray(path, transientContainer, writer, gameInformation);
                }

                _fileSystemAccess.FileMove(tempPath, path);
            }
        }

        // --- FileSystemWatcher integration ---

        private void StartWatching()
        {
            if (_watcher == null)
                return;

            _watcher.Path = SystemFilePath!;
            _watcher.IncludeSubdirectories = true;
            _watcher.Created += OnExternalFileCreated;
            _watcher.Deleted += OnExternalFileDeleted;
            _watcher.Renamed += OnExternalFileRenamed;
            _watcher.EnableRaisingEvents = true;
            _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void OnExternalFileCreated(object? sender, FileSystemEventArgs e)
        {
            if (_suppressWatcher) return;
            lock (_pendingEvents) { _pendingEvents.Add(e); }
            ResetDebounceTimer();
        }

        private void OnExternalFileDeleted(object? sender, FileSystemEventArgs e)
        {
            if (_suppressWatcher) return;
            lock (_pendingEvents) { _pendingEvents.Add(e); }
            ResetDebounceTimer();
        }

        private void OnExternalFileRenamed(object? sender, RenamedEventArgs e)
        {
            if (_suppressWatcher) return;
            lock (_pendingEvents) { _pendingEvents.Add(e); }
            ResetDebounceTimer();
        }

        private void ResetDebounceTimer()
        {
            _debounceTimer?.Change(300, Timeout.Infinite);
        }

        private void OnDebounceElapsed(object? state)
        {
            if (_syncContext != null)
                _syncContext.Post(_ => ProcessPendingEvents(null), null);
            else
                ProcessPendingEvents(null);
        }

        internal void ProcessPendingEvents(object? state)
        {
            if (_disposed)
                return;

            List<FileSystemEventArgs> events;
            lock (_pendingEvents)
            {
                events = new List<FileSystemEventArgs>(_pendingEvents);
                _pendingEvents.Clear();
            }

            var addedFiles = new List<PackFile>();
            var keysToRemove = new List<string>();

            foreach (var e in events)
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        HandleExternalCreated(e.FullPath, addedFiles);
                        break;
                    case WatcherChangeTypes.Deleted:
                        CollectExternalDeleted(e.FullPath, keysToRemove);
                        break;
                    case WatcherChangeTypes.Renamed:
                        var renamedArgs = (RenamedEventArgs)e;
                        CollectExternalDeleted(renamedArgs.OldFullPath, keysToRemove);
                        HandleExternalCreated(renamedArgs.FullPath, addedFiles);
                        break;
                }
            }

            // Publish removed event BEFORE removing from _fileList so that
            // GetFullPath can still resolve paths for the tree view update.
            // Deduplicate keys: chatty/duplicate watcher events (or a folder delete
            // combined with a child delete) can collect the same key more than once.
            var distinctKeysToRemove = keysToRemove.Distinct().ToList();
            var removedFiles = distinctKeysToRemove.Select(k => _fileList[k]).ToList();
            if (removedFiles.Count > 0)
                _eventHub?.PublishGlobalEvent(new PackFileContainerFilesRemovedEvent(this, removedFiles));

            // Now actually remove the entries
            foreach (var key in distinctKeysToRemove)
                _fileList.Remove(key);

            if (addedFiles.Count > 0)
                _eventHub?.PublishGlobalEvent(new PackFileContainerFilesAddedEvent(this, addedFiles));
        }

        private void HandleExternalCreated(string absolutePath, List<PackFile> addedFiles)
        {
            // If this is a directory, scan and add all files within it
            if (Directory.Exists(absolutePath) && !File.Exists(absolutePath))
            {
                try
                {
                    var filesInDir = Directory.GetFiles(absolutePath, "*.*", SearchOption.AllDirectories);
                    foreach (var filePath in filesInDir)
                    {
                        HandleExternalCreated(filePath, addedFiles);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Here().Warning($"Failed to scan externally created directory '{absolutePath}': {ex.Message}");
                }
                return;
            }

            var relativePath = Path.GetRelativePath(SystemFilePath!, absolutePath);
            var normalizedPath = PathNormalization.NormalizeFileName(relativePath);

            if (_fileList.ContainsKey(normalizedPath))
                return; // Already tracked

            try
            {
                var fileName = Path.GetFileName(absolutePath);
                var packFile = new PackFile(fileName, new FileSystemSource(absolutePath));
                _fileList[normalizedPath] = packFile;
                addedFiles.Add(packFile);
            }
            catch (Exception ex)
            {
                // File may be locked or still being written by another process
                _logger.Here().Warning($"Failed to add externally created file '{absolutePath}': {ex.Message}");
            }
        }

        private void CollectExternalDeleted(string absolutePath, List<string> keysToRemove)
        {
            var relativePath = Path.GetRelativePath(SystemFilePath!, absolutePath);
            var normalizedPath = PathNormalization.NormalizeFileName(relativePath);

            // Check if this is an exact file match
            if (_fileList.ContainsKey(normalizedPath))
            {
                keysToRemove.Add(normalizedPath);
                return;
            }

            // Handle folder deletion — collect all files with this prefix
            var folderPrefix = normalizedPath + "\\";
            var matchingKeys = _fileList.Keys
                .Where(k => k.StartsWith(folderPrefix, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            keysToRemove.AddRange(matchingKeys);
        }

        // --- Watcher suppression ---

        private WatcherSuppression SuppressWatcher() => new(this);

        private readonly struct WatcherSuppression : IDisposable
        {
            private readonly SystemFolderContainer _container;

            public WatcherSuppression(SystemFolderContainer container)
            {
                _container = container;
                _container._suppressWatcher = true;
            }

            public void Dispose() => _container._suppressWatcher = false;
        }

        // --- IDisposable ---

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _fileList.Clear();
        }
    }
}
