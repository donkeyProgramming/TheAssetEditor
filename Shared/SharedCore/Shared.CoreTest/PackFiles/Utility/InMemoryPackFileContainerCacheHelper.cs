using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Serialization.CacheDatabase;

namespace Shared.CoreTest.PackFiles.Utility
{
    /// <summary>
    /// An in-memory implementation of IPackFileContainerCacheHelper that avoids all filesystem I/O.
    /// Uses shared in-memory SQLite databases keyed by a logical cache file path.
    /// </summary>
    internal class InMemoryPackFileContainerCacheHelper : IPackFileContainerCacheHelper
    {
        private readonly PackFileContainerCacheHelper _fingerprintHelper = new();

        // Keeps shared in-memory SQLite connections alive (shared cache requires at least one open connection)
        private readonly Dictionary<string, SqliteConnection> _keepAliveConnections = new(StringComparer.OrdinalIgnoreCase);

        // Tracks which logical cache paths have been "saved" (i.e. exist)
        private readonly HashSet<string> _existingCaches = new(StringComparer.OrdinalIgnoreCase);

        // Tracks which caches have been "corrupted" (for simulating corruption scenarios)
        private readonly HashSet<string> _corruptedCaches = new(StringComparer.OrdinalIgnoreCase);

        public string ComputeFingerprint(List<string> packFileNames)
            => _fingerprintHelper.ComputeFingerprint(packFileNames);

        public string GetCacheFilePath(string gameName, string cacheId)
        {
            // Return a logical key, not a real filesystem path
            return $"inmemory://{gameName}_{cacheId}";
        }

        public CachedPackFileContainer SaveAndLoadCache(string fingerprint, PackFileContainer container, string cacheFilePath)
        {
            var dbOptions = GetOrCreateDbOptions(cacheFilePath);
            using (var temp = new CachedPackFileContainer(container.Name, dbOptions))
            {
                temp.Save(fingerprint, container);
            }

            _existingCaches.Add(cacheFilePath);
            _corruptedCaches.Remove(cacheFilePath);

            var loaded = CachedPackFileContainer.CreateFromFingerPrint(dbOptions, fingerprint);
            if (loaded == null)
                throw new Exception($"Failed to load from cache after saving. CacheFile: {cacheFilePath}");

            return loaded;
        }

        public CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint)
        {
            if (!_existingCaches.Contains(cacheFilePath))
                return null;

            if (_corruptedCaches.Contains(cacheFilePath))
                return null;

            try
            {
                var dbOptions = GetOrCreateDbOptions(cacheFilePath);
                return CachedPackFileContainer.CreateFromFingerPrint(dbOptions, fingerprint);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Simulates cache corruption for a given logical path.
        /// </summary>
        public void CorruptCache(string cacheFilePath)
        {
            _corruptedCaches.Add(cacheFilePath);
        }

        public void Dispose()
        {
            foreach (var conn in _keepAliveConnections.Values)
                conn.Dispose();

            _keepAliveConnections.Clear();
            _existingCaches.Clear();
            _corruptedCaches.Clear();
        }

        private DbContextOptions<CacheDbContext> GetOrCreateDbOptions(string logicalPath)
        {
            if (!_keepAliveConnections.TryGetValue(logicalPath, out var connection))
            {
                var dbName = "InMemCache_" + Guid.NewGuid().ToString("N");
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbName,
                    Mode = SqliteOpenMode.Memory,
                    Cache = SqliteCacheMode.Shared
                }.ToString();

                connection = new SqliteConnection(connectionString);
                connection.Open();
                _keepAliveConnections[logicalPath] = connection;
            }

            return new DbContextOptionsBuilder<CacheDbContext>()
                .UseSqlite(connection)
                .Options;
        }
    }
}
