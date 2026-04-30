using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.PackFiles.Serialization
{
    internal static class PackFileContainerCacheHelper
    {
        public static string GetCacheFilePath(string gameDataFolder, string gameName, string cacheId)
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AssetEditor",
                "Cache");

            Directory.CreateDirectory(cacheDir);

            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(cacheDir, $"CachedGameFiles_{safeGameName}_{cacheId}.db");
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

        private const int CurrentSchemaVersion = 2;

        public static void SaveCache(string fingerprint, PackFileContainer container, DbContextOptions<CacheDbContext> dbOptions)
        {
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
                    db.Files.Add(new CachedFileEntity
                    {
                        RelativePath = relativePath,
                        FileName = packFile.Name,
                        Extension = Path.GetExtension(relativePath).ToLower(),
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

            db.SaveChanges();
        }

        public static CachedPackFileContainer? LoadContainerFromCache(string dbFilePath, string expectedFingerprint)
        {
            if (!File.Exists(dbFilePath))
                return null;

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
            catch
            {
                return null;
            }

            CacheInfoEntity? cacheInfo;
            try
            {
                cacheInfo = db.CacheInfo.FirstOrDefault();
            }
            catch
            {
                return null;
            }

            if (cacheInfo == null || cacheInfo.SchemaVersion != CurrentSchemaVersion || cacheInfo.Fingerprint != expectedFingerprint)
                return null;

            var container = new CachedPackFileContainer(cacheInfo.ContainerName, dbOptions)
            {
                SystemFilePath = cacheInfo.SystemFilePath,
            };

            if (!string.IsNullOrEmpty(cacheInfo.SourcePackFilePaths))
            {
                foreach (var path in cacheInfo.SourcePackFilePaths.Split('|'))
                    container.SourcePackFilePaths.Add(path);
            }

            return container;
        }
    }
}
