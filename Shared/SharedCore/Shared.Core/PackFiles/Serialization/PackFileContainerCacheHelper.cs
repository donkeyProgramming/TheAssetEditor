using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using SharpDX.Direct3D9;

namespace Shared.Core.PackFiles.Serialization
{
    internal static class PackFileContainerCacheHelper
    {
        private static readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileContainerCacheHelper));
        public static string GetCacheFilePath(string gameDataFolder, string gameName, string cacheId)
        {
            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(DirectoryHelper.CacheDirectory, $"CachedGameFiles_{safeGameName}_{cacheId}.db");
        }

        public static string ComputeFingerprint(string gameDataFolder, List<string> packFileNames)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();

            foreach (var packFileName in packFileNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var fullPath = Path.Combine(gameDataFolder, packFileName);
                if (File.Exists(fullPath))
                {
                    var info = new FileInfo(fullPath);
                    sb.Append(packFileName);
                    sb.Append('|');
                    sb.Append(info.Length);
                    sb.Append('|');
                    sb.Append(info.LastWriteTimeUtc.Ticks);
                    sb.Append(';');
                }
            }

            var manifestPath = Path.Combine(gameDataFolder, "manifest.txt");
            if (File.Exists(manifestPath))
            {
                var info = new FileInfo(manifestPath);
                sb.Append("manifest.txt|");
                sb.Append(info.Length);
                sb.Append('|');
                sb.Append(info.LastWriteTimeUtc.Ticks);
            }

            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash);
        }

        public static DbContextOptions<CacheDbContext> CreateDbOptions(string dbFilePath)
        {
            return new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite($"Data Source={dbFilePath};Pooling=False")
                .Options;
        }

        private const int CurrentSchemaVersion = 3;

        public static void SaveCache(string fingerprint, PackFileContainer container, DbContextOptions<CacheDbContext> dbOptions)
        {
            _logger.Here().Information($"Saving cache for '{container.Name}' with {container.GetFileCount()} files");
            using var db = new CacheDbContext(dbOptions);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var sourcePackFilePaths = string.Join("|", container.SourcePackFilePaths);

            db.CacheInfo.Add(new CacheInfoEntity
            {
                SchemaVersion = CurrentSchemaVersion,
                Fingerprint = fingerprint,
                ContainerName = container.Name,
                SystemFilePath = container.SystemFilePath,
                SourcePackFilePaths = sourcePackFilePaths,
            });

            foreach (var (relativePath, packFile) in container.GetAllFiles())
            {
                if (packFile.DataSource is PackedFileSource source)
                {
                    var lastSep = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                    var folderPath = lastSep == -1 ? "" : relativePath.Substring(0, lastSep);

                    db.Files.Add(new CachedFileEntity
                    {
                        RelativePath = relativePath,
                        FileName = packFile.Name,
                        Extension = Path.GetExtension(relativePath).ToLower(),
                        FolderPath = folderPath,
                        SourcePackFilePath = source.Parent.FilePath,
                        Offset = source.Offset,
                        Size = source.Size,
                        IsEncrypted = source.IsEncrypted,
                        IsCompressed = source.IsCompressed,
                        CompressionFormat = (int)source.CompressionFormat,
                        UncompressedSize = source.UncompressedSize,
                    });
                }
            }

            _logger.Here().Information($"Starting to save cache for '{container.Name}'");
            db.SaveChanges();
            _logger.Here().Information($"Cache saved successfully for '{container.Name}'");
        }

        public static CachedPackFileContainer? LoadContainerFromCache(string dbFilePath, string expectedFingerprint)
        {
            if (!File.Exists(dbFilePath))
            {
                _logger.Here().Information($"No cache file found at '{dbFilePath}'");
                return null;
            }

            var dbOptions = CreateDbOptions(dbFilePath);
            return LoadContainerFromCache(dbOptions, expectedFingerprint);
        }

        public static CachedPackFileContainer? LoadContainerFromCache(DbContextOptions<CacheDbContext> dbOptions, string expectedFingerprint)
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

        public static CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint)
        {
            try
            {
                return LoadContainerFromCache(cacheFilePath, fingerprint);
            }
            catch (Exception ex)
            {
                _logger.Here().Warning($"Failed to load from cache '{cacheFilePath}': {ex.Message}");
                return null;
            }
        }
    }
}
