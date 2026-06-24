using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models.Containers
{
    internal enum CacheStorageMode
    {
        File,
        InMemory
    }

    internal class CachedPackFileContainer : IPackFileContainerInternal, IDisposable
    {
        private static readonly ILogger _logger = Logging.CreateStatic(typeof(CachedPackFileContainer));
        private const int CurrentSchemaVersion = 3;

        private CacheDbContext _db;
        private readonly DbContextOptions<CacheDbContext> _dbOptions;
        private readonly object _dbLock = new();
        private SqliteConnection? _keepAliveConnection;

        public CacheStorageMode StorageMode { get; }
        public string? DbFilePath { get; }
        public string Name { get; set; }
        public bool IsReadOnly { get => true; set { } }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public PackFileContainerType ContainerType => PackFileContainerType.Database;
        public HashSet<string> SourcePackFilePaths { get; set; } = [];

        /// <summary>
        /// Creates a file-backed CachedPackFileContainer.
        /// </summary>
        public CachedPackFileContainer(string name, string dbFilePath)
        {
            Name = name;
            SystemFilePath = string.Empty;
            DbFilePath = dbFilePath;
            StorageMode = CacheStorageMode.File;
            _dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite($"Data Source={dbFilePath};Pooling=False")
                .Options;
            _db = new CacheDbContext(_dbOptions);
            _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        /// <summary>
        /// Creates an in-memory CachedPackFileContainer.
        /// </summary>
        public CachedPackFileContainer(string name, DbContextOptions<CacheDbContext> dbOptions)
        {
            Name = name;
            SystemFilePath = string.Empty;
            DbFilePath = null;
            StorageMode = CacheStorageMode.InMemory;
            _dbOptions = dbOptions;
            _db = new CacheDbContext(dbOptions);
            _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        /// <summary>
        /// Saves the contents of a PackFileContainer into this cached container's database.
        /// </summary>
        public void Save(string fingerprint, PackFileContainer source)
        {
            _logger.Here().Information($"Saving cache for '{source.Name}' with {source.GetFileCount()} files");

            // Use EF to create the schema
            using (var db = new CacheDbContext(_dbOptions))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            var (connection, shouldDisposeConnection) = GetSqliteConnection(_dbOptions);
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            try
            {
                using var transaction = connection.BeginTransaction();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT INTO CacheInfo (SchemaVersion, Fingerprint, ContainerName, SystemFilePath, SourcePackFilePaths)
                                    VALUES ($schemaVersion, $fingerprint, $containerName, $systemFilePath, $sourcePackFilePaths)";
                    cmd.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
                    cmd.Parameters.AddWithValue("$fingerprint", fingerprint);
                    cmd.Parameters.AddWithValue("$containerName", source.Name);
                    cmd.Parameters.AddWithValue("$systemFilePath", source.SystemFilePath);
                    cmd.Parameters.AddWithValue("$sourcePackFilePaths", string.Join("|", source.SourcePackFilePaths));
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT INTO FileList (RelativePath, FileName, Extension, FolderPath, SourcePackFilePath, Offset, Size, IsEncrypted, IsCompressed, CompressionFormat, UncompressedSize)
                                    VALUES ($relativePath, $fileName, $extension, $folderPath, $sourcePackFilePath, $offset, $size, $isEncrypted, $isCompressed, $compressionFormat, $uncompressedSize)";

                    var pRelativePath = cmd.Parameters.Add("$relativePath", SqliteType.Text);
                    var pFileName = cmd.Parameters.Add("$fileName", SqliteType.Text);
                    var pExtension = cmd.Parameters.Add("$extension", SqliteType.Text);
                    var pFolderPath = cmd.Parameters.Add("$folderPath", SqliteType.Text);
                    var pSourcePackFilePath = cmd.Parameters.Add("$sourcePackFilePath", SqliteType.Text);
                    var pOffset = cmd.Parameters.Add("$offset", SqliteType.Integer);
                    var pSize = cmd.Parameters.Add("$size", SqliteType.Integer);
                    var pIsEncrypted = cmd.Parameters.Add("$isEncrypted", SqliteType.Integer);
                    var pIsCompressed = cmd.Parameters.Add("$isCompressed", SqliteType.Integer);
                    var pCompressionFormat = cmd.Parameters.Add("$compressionFormat", SqliteType.Integer);
                    var pUncompressedSize = cmd.Parameters.Add("$uncompressedSize", SqliteType.Integer);

                    cmd.Prepare();

                    foreach (var (relativePath, packFile) in source.GetAllFiles())
                    {
                        if (packFile.DataSource is not PackedFileSource fileSource)
                            continue;

                        var lastSep = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                        var folderPath = lastSep == -1 ? "" : relativePath.Substring(0, lastSep);

                        pRelativePath.Value = relativePath;
                        pFileName.Value = packFile.Name;
                        pExtension.Value = Path.GetExtension(relativePath).ToLower();
                        pFolderPath.Value = folderPath;
                        pSourcePackFilePath.Value = fileSource.Parent.FilePath;
                        pOffset.Value = fileSource.Offset;
                        pSize.Value = fileSource.Size;
                        pIsEncrypted.Value = fileSource.IsEncrypted ? 1 : 0;
                        pIsCompressed.Value = fileSource.IsCompressed ? 1 : 0;
                        pCompressionFormat.Value = (int)fileSource.CompressionFormat;
                        pUncompressedSize.Value = (long)fileSource.UncompressedSize;

                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                _logger.Here().Information($"Cache saved successfully for '{source.Name}'");
            }
            finally
            {
                if (shouldDisposeConnection)
                    connection.Dispose();
            }

            // Update this container's metadata from what was saved
            Name = source.Name;
            SystemFilePath = source.SystemFilePath ?? string.Empty;
            SourcePackFilePaths = new HashSet<string>(source.SourcePackFilePaths);

            // Recreate _db so this instance can be queried after Save()
            _db.Dispose();
            _db = new CacheDbContext(_dbOptions);
            _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        /// <summary>
        /// Loads a CachedPackFileContainer from a file-backed database.
        /// Returns null if the file doesn't exist, the DB is corrupt, or the fingerprint doesn't match.
        /// </summary>
        public static CachedPackFileContainer? CreateFromFingerPrint(string dbFilePath, string expectedFingerprint)
        {
            if (!File.Exists(dbFilePath))
            {
                _logger.Here().Information($"No cache file found at '{dbFilePath}'");
                return null;
            }

            var dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite($"Data Source={dbFilePath};Pooling=False")
                .Options;

            return CreateFromFingerPrint(dbOptions, expectedFingerprint);
        }

        /// <summary>
        /// Loads a CachedPackFileContainer from the given DbContextOptions (file or in-memory).
        /// Returns null if the DB is corrupt or the fingerprint doesn't match.
        /// </summary>
        public static CachedPackFileContainer? CreateFromFingerPrint(DbContextOptions<CacheDbContext> dbOptions, string expectedFingerprint)
        {
            using var db = new CacheDbContext(dbOptions);

            try
            {
                db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                _logger.Here().Warning($"Failed to open cache database: {ex.Message}");
                return null;
            }

            CacheInfoEntity? cacheInfo;
            try
            {
                cacheInfo = db.CacheInfo.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.Here().Warning($"Failed to read cache info: {ex.Message}");
                return null;
            }

            if (cacheInfo == null || cacheInfo.SchemaVersion != CurrentSchemaVersion || cacheInfo.Fingerprint != expectedFingerprint)
            {
                _logger.Here().Information($"Cache invalid - schema:{cacheInfo?.SchemaVersion} (expected {CurrentSchemaVersion}), fingerprint match:{cacheInfo?.Fingerprint == expectedFingerprint}");
                return null;
            }

            var container = new CachedPackFileContainer(cacheInfo.ContainerName, dbOptions)
            {
                SystemFilePath = cacheInfo.SystemFilePath,
            };

            if (!string.IsNullOrEmpty(cacheInfo.SourcePackFilePaths))
            {
                foreach (var path in cacheInfo.SourcePackFilePaths.Split('|'))
                    container.SourcePackFilePaths.Add(path);
            }

            _logger.Here().Information($"Loaded container '{container.Name}' from cache");
            return container;
        }

        private static (SqliteConnection Connection, bool ShouldDisposeConnection) GetSqliteConnection(DbContextOptions<CacheDbContext> dbOptions)
        {
            var relationalOptions = dbOptions.Extensions
                .OfType<RelationalOptionsExtension>()
                .FirstOrDefault();

            if (relationalOptions?.Connection is SqliteConnection sqliteConnection)
                return (sqliteConnection, false);

            var connectionString = relationalOptions?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Unable to resolve SQLite connection from DbContextOptions.");

            return (new SqliteConnection(connectionString), true);
        }

        /// <summary>
        /// Creates a CachedPackFileContainer from a list of file entries. Useful for testing.
        /// </summary>
        public static CachedPackFileContainer CreateFromFileList(
            string containerName,
            (string RelativePath, string FileName, long Offset, long Size, bool IsEncrypted, bool IsCompressed, CompressionFormat CompressionFormat, uint UncompressedSize)[] files,
            bool useInMemoryDb,
            string? dbFilePath = null,
            string systemFilePath = "",
            string? sourcePackFilePath = null)
        {
            var packParent = new PackedFileSourceParent { FilePath = sourcePackFilePath ?? @"c:\game\data\pack1.pack" };

            var source = PackFileContainer.CreateReadOnlyPackFile(containerName, systemFilePath);

            if (sourcePackFilePath != null)
                source.SourcePackFilePaths.Add(sourcePackFilePath);
            else
                source.SourcePackFilePaths.Add(packParent.FilePath);

            foreach (var file in files)
                source.AddOrUpdateFile(file.RelativePath, new PackFile(file.FileName, new PackedFileSource(packParent, file.Offset, file.Size, file.IsEncrypted, file.IsCompressed, file.CompressionFormat, file.UncompressedSize)));

            DbContextOptions<CacheDbContext> dbOptions;
            SqliteConnection? keepAlive = null;
            if (useInMemoryDb)
            {
                var dbName = "CachedContainer_" + Guid.NewGuid().ToString("N");
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbName,
                    Mode = SqliteOpenMode.Memory,
                    Cache = SqliteCacheMode.Shared
                }.ToString();

                // Open a keep-alive connection for shared in-memory SQLite
                keepAlive = new SqliteConnection(connectionString);
                keepAlive.Open();

                dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
                    .UseSqlite(connectionString)
                    .Options;
            }
            else
            {
                dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
                    .UseSqlite($"Data Source={dbFilePath};Pooling=False")
                    .Options;
            }

            var fingerprint = "factory_fp";
            using (var temp = new CachedPackFileContainer(containerName, dbOptions))
            {
                temp.Save(fingerprint, source);
            }

            var result = CreateFromFingerPrint(dbOptions, fingerprint)!;
            result._keepAliveConnection = keepAlive;
            return result;
        }

        public void Dispose()
        {
            _db.Dispose();
            _keepAliveConnection?.Dispose();
            _keepAliveConnection = null;
        }

        public int GetFileCount()
        {
            lock (_dbLock)
            {
                return _db.Files.Count();
            }
        }

        public PackFile? FindFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            CachedFileEntity? entry;
            lock (_dbLock)
            {
                entry = _db.Files.FirstOrDefault(f => f.RelativePath == lowerPath);
            }

            return entry != null ? ToPackFile(entry) : null;
        }

        public bool ContainsFile(string path)
        {
            var lowerPath = PathNormalization.NormalizeFileName(path);
            lock (_dbLock)
            {
                return _db.Files.Any(f => f.RelativePath == lowerPath);
            }
        }

        public string? GetFullPath(PackFile file)
        {
            if (file.DataSource is PackedFileSource source)
            {
                CachedFileEntity? entry;
                lock (_dbLock)
                {
                    entry = _db.Files.FirstOrDefault(f =>
                        f.SourcePackFilePath == source.Parent.FilePath &&
                        f.Offset == source.Offset &&
                        f.Size == source.Size &&
                        f.IsEncrypted == source.IsEncrypted &&
                        f.IsCompressed == source.IsCompressed &&
                        f.CompressionFormat == (int)source.CompressionFormat &&
                        f.UncompressedSize == source.UncompressedSize &&
                        f.FileName.ToLower() == file.Name.ToLower());
                }

                if (entry != null)
                    return entry.RelativePath;
            }

            List<string> matchingPaths;
            lock (_dbLock)
            {
                matchingPaths = _db.Files
                    .Where(f => f.FileName.ToLower() == file.Name.ToLower())
                    .Select(f => f.RelativePath)
                    .Take(2)
                    .ToList();
            }

            return matchingPaths.Count == 1 ? matchingPaths[0] : null;
        }

        public List<(string FileName, PackFile Pack)> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            List<CachedFileEntity> entries;
            lock (_dbLock)
            {
                entries = _db.Files
                    .Where(f => f.Extension == extention)
                    .ToList();
            }

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public List<(string Path, PackFile File)> SearchFiles(string? textFilter, IReadOnlyList<string>? extensions)
        {
            List<CachedFileEntity> entries;
            lock (_dbLock)
            {
                IQueryable<CachedFileEntity> query = _db.Files;

                if (extensions != null && extensions.Count > 0)
                {
                    var extList = extensions.Select(e => e.ToLowerInvariant()).ToList();
                    query = query.Where(f => extList.Any(ext => f.FileName.Contains(ext)));
                }

                if (!string.IsNullOrWhiteSpace(textFilter))
                {
                    var pattern = $"%{textFilter}%";
                    query = query.Where(f => EF.Functions.Like(f.FileName, pattern));
                }

                entries = query.OrderBy(f => f.RelativePath).ToList();
            }

            return entries.Select(e => (e.RelativePath, ToPackFile(e))).ToList();
        }

        public SortedDictionary<string, List<string>> GetAllFilesByFolder()
        {
            var time = Stopwatch.StartNew();
            lock (_dbLock)
            {
                var rows = _db.Files
                    .Select(f => new { f.FolderPath, f.FileName })
                    .ToList();

                var sorted = new SortedDictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var group in rows.GroupBy(f => f.FolderPath))
                {
                    var files = group.Select(f => f.FileName).ToList();
                    PackFileSortHelper.SortFileList(files);
                    sorted[group.Key] = files;
                }

                _logger.Here().Information("Getting all files from cached container took {ElapsedMilliseconds} ms", time.ElapsedMilliseconds);
                return sorted;
            }
        }

        public Dictionary<string, PackFile> GetAllFiles()
        {
            var time = Stopwatch.StartNew();
            List<CachedFileEntity> entries;
            lock (_dbLock)
            {
                entries = _db.Files.ToList();
            }
            _logger.Here().Information("Getting all files from cached container took {ElapsedMilliseconds} ms", time.ElapsedMilliseconds);

            var parentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, PackFile>(entries.Count);

            foreach (var entry in entries)
            {
                if (!parentCache.TryGetValue(entry.SourcePackFilePath, out var parent))
                {
                    parent = new PackedFileSourceParent { FilePath = entry.SourcePackFilePath };
                    parentCache[entry.SourcePackFilePath] = parent;
                }

                var source = new PackedFileSource(
                    parent, entry.Offset, entry.Size,
                    entry.IsEncrypted, entry.IsCompressed,
                    (Utility.CompressionFormat)entry.CompressionFormat, entry.UncompressedSize);

                result[entry.RelativePath] = new PackFile(entry.FileName, source);
            }

            _logger.Here().Information("Getting all files and processing from cached container took {ElapsedMilliseconds} ms", time.ElapsedMilliseconds);

            return result;
        }



        public List<(string Path, PackFile File)> GetDirectoryContent(string directoryPath)
        {
            List<DirectoryFileRow> directFileRows;
            lock (_dbLock)
            {
                directFileRows = _db.Files
                    .Where(f => f.FolderPath == directoryPath)
                    .Select(f => new DirectoryFileRow
                    {
                        FolderPath = f.FolderPath,
                        FileName = f.FileName,
                        SourcePackFilePath = f.SourcePackFilePath,
                        Offset = f.Offset,
                        Size = f.Size,
                        IsEncrypted = f.IsEncrypted,
                        IsCompressed = f.IsCompressed,
                        CompressionFormat = f.CompressionFormat,
                        UncompressedSize = f.UncompressedSize
                    })
                    .ToList();
            }

            var packedFileSourceParentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);
            var files = directFileRows
                .Select(f =>
                {
                    if (!packedFileSourceParentCache.TryGetValue(f.SourcePackFilePath, out var parent))
                    {
                        parent = new PackedFileSourceParent { FilePath = f.SourcePackFilePath };
                        packedFileSourceParentCache[f.SourcePackFilePath] = parent;
                    }

                    var source = new PackedFileSource(parent, f.Offset, f.Size, f.IsEncrypted, f.IsCompressed, (Utility.CompressionFormat)f.CompressionFormat, f.UncompressedSize);
                    var relativePath = string.IsNullOrEmpty(f.FolderPath) ? f.FileName : $"{f.FolderPath}\\{f.FileName}";
                    return (Path: relativePath, File: new PackFile(f.FileName, source));
                })
                .OrderBy(x => x.Path, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return files;
        }

        public void AddOrUpdateFile(string path, PackFile file) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public List<PackFile> AddFiles(List<NewPackFileEntry> newFiles) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public PackFile? DeleteFile(PackFile file) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void DeleteFolder(string folder) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void MoveFile(PackFile file, string newFolderPath) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public string RenameDirectory(string currentNodeName, string newName) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void RenameFile(PackFile file, string newName) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void SaveFileData(PackFile file, byte[] data) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        public void SaveToDisk(string path, bool createBackup, GameInformation gameInformation) =>
            throw new InvalidOperationException("Cannot modify a cached CA pack file container.");

        private static PackFile ToPackFile(CachedFileEntity entry)
        {
            var parent = new PackedFileSourceParent { FilePath = entry.SourcePackFilePath };
            var source = new PackedFileSource(
                parent, entry.Offset, entry.Size,
                entry.IsEncrypted, entry.IsCompressed,
                (Utility.CompressionFormat)entry.CompressionFormat, entry.UncompressedSize);
            return new PackFile(entry.FileName, source);
        }

        private sealed class DirectoryFileRow
        {
            public string FolderPath { get; init; } = string.Empty;
            public string FileName { get; init; } = string.Empty;
            public string SourcePackFilePath { get; init; } = string.Empty;
            public long Offset { get; init; }
            public long Size { get; init; }
            public bool IsEncrypted { get; init; }
            public bool IsCompressed { get; init; }
            public int CompressionFormat { get; init; }
            public uint UncompressedSize { get; init; }
        }
    }
}
