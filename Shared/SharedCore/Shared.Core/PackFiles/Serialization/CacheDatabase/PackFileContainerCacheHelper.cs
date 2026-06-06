using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;

namespace Shared.Core.PackFiles.Serialization.CacheDatabase
{
    interface IPackFileContainerCacheHelper
    {
        string ComputeFingerprint(List<string> packFileNames);
        DbContextOptions<CacheDbContext> CreateDbOptions(SqliteConnection connection);
        DbContextOptions<CacheDbContext> CreateDbOptions(string dbFilePath);
        DbContextOptions<CacheDbContext> CreateDbOptionsFromConnectionString(string connectionString);
        string GetCacheFilePath(string gameName, string cacheId);
        CachedPackFileContainer? LoadContainerFromCache(DbContextOptions<CacheDbContext> dbOptions, string expectedFingerprint);
        CachedPackFileContainer? LoadContainerFromCache(string dbFilePath, string expectedFingerprint);
        void SaveCache(string fingerprint, PackFileContainer container, DbContextOptions<CacheDbContext> dbOptions);
        CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint);
    }

    class PackFileContainerCacheHelper : IPackFileContainerCacheHelper
    {
        private readonly ILogger _logger = Logging.Create<PackFileContainerCacheHelper>();
        public string GetCacheFilePath(string gameName, string cacheId)
        {
            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(DirectoryHelper.CacheDirectory, $"CachedGameFiles_{safeGameName}_{cacheId}.db");
        }

        public string ComputeFingerprint(List<string> packFileNames)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();

            if (packFileNames.Count == 0)
                throw new Exception("Trying to compute CachecFingerPrint, but no files provied");

            var foundFiles = 0;
            foreach (var packFileName in packFileNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(packFileName))
                {
                    foundFiles++;

                    var info = new FileInfo(packFileName);
                    sb.Append(packFileName);
                    sb.Append('|');
                    sb.Append(info.Length);
                    sb.Append('|');
                    sb.Append(info.LastWriteTimeUtc.Ticks);
                    sb.Append(';');
                }
                else
                {
                    _logger.Here().Warning($"Trying to compute CachecFingerPrint, but file {packFileName} is not found");
                }
            }

            if (foundFiles == 0)
                throw new Exception("Trying to compute CachecFingerPrint, but no files found. This will result in a default ID which can cause problems");

            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash);
        }

        public DbContextOptions<CacheDbContext> CreateDbOptions(string dbFilePath)
        {
            return new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite($"Data Source={dbFilePath};Pooling=False")
                .Options;
        }

        public DbContextOptions<CacheDbContext> CreateDbOptionsFromConnectionString(string connectionString)
        {
            return new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite(connectionString)
                .Options;
        }

        public DbContextOptions<CacheDbContext> CreateDbOptions(SqliteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            return new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        private const int CurrentSchemaVersion = 3;

        public void SaveCache(string fingerprint, PackFileContainer container, DbContextOptions<CacheDbContext> dbOptions)
        {
            _logger.Here().Information($"Saving cache for '{container.Name}' with {container.GetFileCount()} files");

            // Use EF only to create the schema, then dispose to release memory
            using (var db = new CacheDbContext(dbOptions))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            // Use raw SQLite for bulk insert - avoids EF change tracker memory and perf overhead.
            // If dbOptions carries an explicit SqliteConnection (for tests/in-memory), reuse it.
            var (connection, shouldDisposeConnection) = GetSqliteConnection(dbOptions);
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            try
            {
                using var transaction = connection.BeginTransaction();

                // Insert CacheInfo
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT INTO CacheInfo (SchemaVersion, Fingerprint, ContainerName, SystemFilePath, SourcePackFilePaths)
                                    VALUES ($schemaVersion, $fingerprint, $containerName, $systemFilePath, $sourcePackFilePaths)";
                    cmd.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
                    cmd.Parameters.AddWithValue("$fingerprint", fingerprint);
                    cmd.Parameters.AddWithValue("$containerName", container.Name);
                    cmd.Parameters.AddWithValue("$systemFilePath", container.SystemFilePath);
                    cmd.Parameters.AddWithValue("$sourcePackFilePaths", string.Join("|", container.SourcePackFilePaths));
                    cmd.ExecuteNonQuery();
                }

                // Bulk insert files using a prepared statement
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

                    foreach (var (relativePath, packFile) in container.GetAllFiles())
                    {
                        if (packFile.DataSource is not PackedFileSource source)
                            continue;

                        var lastSep = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                        var folderPath = lastSep == -1 ? "" : relativePath.Substring(0, lastSep);

                        pRelativePath.Value = relativePath;
                        pFileName.Value = packFile.Name;
                        pExtension.Value = Path.GetExtension(relativePath).ToLower();
                        pFolderPath.Value = folderPath;
                        pSourcePackFilePath.Value = source.Parent.FilePath;
                        pOffset.Value = source.Offset;
                        pSize.Value = source.Size;
                        pIsEncrypted.Value = source.IsEncrypted ? 1 : 0;
                        pIsCompressed.Value = source.IsCompressed ? 1 : 0;
                        pCompressionFormat.Value = (int)source.CompressionFormat;
                        pUncompressedSize.Value = (long)source.UncompressedSize;

                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                _logger.Here().Information($"Cache saved successfully for '{container.Name}'");
            }
            finally
            {
                if (shouldDisposeConnection)
                    connection.Dispose();
            }
        }

        private (SqliteConnection Connection, bool ShouldDisposeConnection) GetSqliteConnection(DbContextOptions<CacheDbContext> dbOptions)
        {
            var relationalOptions = dbOptions.Extensions
                .OfType<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
                .FirstOrDefault();

            if (relationalOptions?.Connection is SqliteConnection sqliteConnection)
                return (sqliteConnection, false);

            var connectionString = relationalOptions?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Unable to resolve SQLite connection from DbContextOptions.");

            return (new SqliteConnection(connectionString), true);
        }

        public CachedPackFileContainer? LoadContainerFromCache(string dbFilePath, string expectedFingerprint)
        {
            if (!File.Exists(dbFilePath))
            {
                _logger.Here().Information($"No cache file found at '{dbFilePath}'");
                return null;
            }

            var dbOptions = CreateDbOptions(dbFilePath);
            return LoadContainerFromCache(dbOptions, expectedFingerprint);
        }

        public CachedPackFileContainer? LoadContainerFromCache(DbContextOptions<CacheDbContext> dbOptions, string expectedFingerprint)
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

        public CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint)
        {
            if (!File.Exists(cacheFilePath))
            {
                _logger.Here().Information($"Cache file does not exist: {cacheFilePath}");
                return null;
            }

            try
            {
                _logger.Here().Information($"Attempting to load cache from: {cacheFilePath} with fingerprint: {fingerprint}");
                var result = LoadContainerFromCache(cacheFilePath, fingerprint);
                if (result == null)
                    _logger.Here().Information($"Cache load returned null (fingerprint/schema mismatch) for: {cacheFilePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Here().Warning($"Failed to load from cache '{cacheFilePath}': {ex.Message}");
                return null;
            }
        }
    }
}
